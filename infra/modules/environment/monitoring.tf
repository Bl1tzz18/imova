# Logs and telemetry (docs/development.md, "Monitoring"): one Log Analytics workspace holds both
# Application Insights' data and the Container Apps' console logs. 30 days' retention (the privacy
# policy promises at most 90) and a daily cap. The first 5 GB a month are free.

resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.name}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = var.log_daily_cap_gb
  tags                = local.tags
}

resource "azurerm_application_insights" "main" {
  name                 = "appi-${local.name}"
  resource_group_name  = azurerm_resource_group.main.name
  location             = azurerm_resource_group.main.location
  workspace_id         = azurerm_log_analytics_workspace.main.id
  application_type     = "web"
  retention_in_days    = 30
  daily_data_cap_in_gb = var.log_daily_cap_gb
  # Client IPs are dropped on ingestion; never turn this off (privacy policy).
  ip_masking_enabled = true
  tags               = local.tags
}

# --- Alerts -------------------------------------------------------------------------------------

resource "azurerm_monitor_action_group" "main" {
  count = var.alert_email != null ? 1 : 0

  name                = "ag-${local.name}"
  resource_group_name = azurerm_resource_group.main.name
  short_name          = "imova${var.environment}"

  email_receiver {
    name                    = "owner"
    email_address           = var.alert_email
    use_common_alert_schema = true
  }

  tags = local.tags
}

# One test, of the home page: it's what visitors see, and it calls the API and the database
# behind it. Every 15 minutes from 3 EU locations ≈ 8,600 runs ≈ $5.60/month (every 5 minutes would
# be ~$17).
resource "azurerm_application_insights_standard_web_test" "home" {
  count = var.availability_test_enabled ? 1 : 0

  name                    = "home-${local.name}"
  resource_group_name     = azurerm_resource_group.main.name
  location                = azurerm_resource_group.main.location
  application_insights_id = azurerm_application_insights.main.id
  frequency               = 900
  timeout                 = 30
  retry_enabled           = true
  enabled                 = true
  geo_locations = [
    "emea-nl-ams-azr",  # West Europe
    "emea-gb-db3-azr",  # North Europe
    "emea-fr-pra-edge", # France Central
  ]

  request {
    url = "${local.web_url}/"
  }

  validation_rules {
    expected_status_code        = 200
    ssl_check_enabled           = true
    ssl_cert_remaining_lifetime = 7
  }

  tags = merge(local.tags, {
    # Application Insights links a web test to its component through this tag.
    "hidden-link:${azurerm_application_insights.main.id}" = "Resource"
  })
}

resource "azurerm_monitor_metric_alert" "home_down" {
  count = var.availability_test_enabled && var.alert_email != null ? 1 : 0

  name                = "home-down-${local.name}"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_application_insights_standard_web_test.home[0].id, azurerm_application_insights.main.id]
  description         = "The home page failed from 2 of 3 locations."
  severity            = 1

  application_insights_web_test_location_availability_criteria {
    web_test_id           = azurerm_application_insights_standard_web_test.home[0].id
    component_id          = azurerm_application_insights.main.id
    failed_location_count = 2
  }

  action {
    action_group_id = azurerm_monitor_action_group.main[0].id
  }

  tags = local.tags
}

# Any exception, or any error log (e.g. a Worker job that failed), in the last 15 minutes. One rule
# for both (each log rule costs ~$0.50/month); at most one email per hour.
resource "azurerm_monitor_scheduled_query_rules_alert_v2" "errors" {
  count = var.error_alert_enabled && var.alert_email != null ? 1 : 0

  name                 = "errors-${local.name}"
  resource_group_name  = azurerm_resource_group.main.name
  location             = azurerm_resource_group.main.location
  scopes               = [azurerm_application_insights.main.id]
  description          = "An exception or an error log in imova-web, imova-api or imova-worker."
  severity             = 2
  evaluation_frequency = "PT15M"
  window_duration      = "PT15M"
  # One email, then quiet for an hour while it keeps failing.
  mute_actions_after_alert_duration = "PT1H"
  auto_mitigation_enabled           = false

  criteria {
    query                   = <<-KQL
      union
        (exceptions),
        (traces | where severityLevel >= 3)
      | where cloud_RoleName in ("imova-web", "imova-api", "imova-worker")
    KQL
    time_aggregation_method = "Count"
    operator                = "GreaterThan"
    threshold               = 0
  }

  action {
    action_groups = [azurerm_monitor_action_group.main[0].id]
  }

  tags = local.tags
}
