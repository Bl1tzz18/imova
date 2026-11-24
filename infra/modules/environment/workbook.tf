# The "IMOVA — trafic și activitate" dashboard: an Azure Monitor workbook on Application Insights
# (free — it only runs queries). Portal: Application Insights → Workbooks, or Monitor → Workbooks.
# What it reads:
# - PageView custom events (web app, lib/monitoring/pageViews.ts): every full page load by a person,
#   whatever they chose in the cookie banner — anonymous, aggregate (page kind, device, language,
#   source domain, utm tags; no id, IP or user agent);
# - the API's business events (traces with customDimensions.Event — AppEvents / CommandLoggingBehavior);
# - exceptions and requests (errors, response times).
# Data is kept 30 days (monitoring.tf), so that's as far back as it can look.

locals {
  workbook_query = {
    version      = "KqlItem/1.0"
    size         = 0
    queryType    = 0
    resourceType = "microsoft.insights/components"
    # Every query follows the time-range pills at the top.
    timeContextFromParameter = "TimeRange"
  }

  # Business events that say how the marketplace is doing (CommandLoggingBehavior logs
  # "<Event> succeeded"; AccountCreated is logged by Register and the first Google sign-in).
  workbook_business_events = ["AccountCreated", "CreateListing", "ApproveListing", "StartConversation", "ReportListing", "MarkListingAsSold", "MarkListingAsRented"]

  workbook = {
    version = "Notebook/1.0"
    items = [
      {
        type = 9
        name = "parameters"
        content = {
          version      = "KqlParameterItem/1.0"
          style        = "pills"
          queryType    = 0
          resourceType = "microsoft.insights/components"
          parameters = [{
            id         = "9b0e6a2e-1f3c-4c1e-9a55-2f1b8d6c0a01"
            version    = "KqlParameterItem/1.0"
            name       = "TimeRange"
            label      = "Perioada"
            type       = 4
            isRequired = true
            value      = { durationMs = 604800000 }
            typeSettings = {
              allowCustom = true
              selectableValues = [
                { durationMs = 86400000 },
                { durationMs = 604800000 },
                { durationMs = 1209600000 },
                { durationMs = 2592000000 },
              ]
            }
          }]
        }
      },
      {
        type    = 1
        name    = "intro"
        content = { json = "## IMOVA — trafic și activitate\nÎncărcările de pagină sunt anonime și includ toți vizitatorii (și pe cei care refuză cookie-urile): fără IP, fără identificatori, doar tipul paginii, dispozitivul, limba și sursa. Click-urile în interiorul site-ului nu sunt aici — pentru ele și pentru vizitatori unici, PostHog (doar cu acord). Datele se păstrează 30 de zile." }
      },
      {
        type = 3
        name = "kpis"
        content = merge(local.workbook_query, {
          title         = "Pe scurt"
          visualization = "tiles"
          query         = <<-KQL
            let views = customEvents | where name == "PageView" | summarize Value = count() | extend Metric = "Încărcări de pagină", Order = 1;
            let entries = customEvents | where name == "PageView" and tostring(customDimensions.source) != "internal" | summarize Value = count() | extend Metric = "Intrări din afară", Order = 2;
            let business = traces
              | extend Event = tostring(customDimensions.Event)
              | where Event in ("AccountCreated", "CreateListing", "ApproveListing", "StartConversation")
              | where Event == "AccountCreated" or message has " succeeded"
              | summarize Value = count() by Event
              | extend Metric = case(Event == "AccountCreated", "Conturi noi", Event == "CreateListing", "Anunțuri create", Event == "ApproveListing", "Anunțuri aprobate", "Conversații începute"),
                       Order = case(Event == "AccountCreated", 3, Event == "CreateListing", 4, Event == "ApproveListing", 5, 6)
              | project Metric, Value, Order;
            let errors = exceptions | summarize Value = count() | extend Metric = "Erori", Order = 7;
            union views, entries, business, errors
            | order by Order asc
            | project Metric, Value
          KQL
          tileSettings = {
            showBorder   = true
            titleContent = { columnMatch = "Metric", formatter = 1 }
            leftContent = {
              columnMatch   = "Value"
              formatter     = 12
              formatOptions = { palette = "auto" }
              numberFormat  = { unit = 17, options = { style = "decimal", maximumFractionDigits = 0 } }
            }
          }
        })
      },
      {
        type = 3
        name = "page-loads-by-device"
        content = merge(local.workbook_query, {
          title         = "Încărcări de pagină pe zi, pe dispozitiv"
          visualization = "timechart"
          query         = <<-KQL
            customEvents
            | where name == "PageView"
            | summarize Loads = count() by bin(timestamp, 1d), Device = tostring(customDimensions.device)
          KQL
        })
      },
      {
        type = 3
        name = "top-pages"
        content = merge(local.workbook_query, {
          title         = "Pagini"
          visualization = "table"
          query         = <<-KQL
            customEvents
            | where name == "PageView"
            | summarize Loads = count(), FromOutside = countif(tostring(customDimensions.source) != "internal") by Page = tostring(customDimensions.page)
            | order by Loads desc
          KQL
        })
        customWidth = "50"
      },
      {
        type = 3
        name = "sources"
        content = merge(local.workbook_query, {
          title         = "De unde vin (intrări din afară)"
          visualization = "barchart"
          query         = <<-KQL
            customEvents
            | where name == "PageView" and tostring(customDimensions.source) != "internal"
            | summarize Entries = count() by Source = tostring(customDimensions.source)
            | top 15 by Entries
          KQL
        })
        customWidth = "50"
      },
      {
        type = 3
        name = "campaigns"
        content = merge(local.workbook_query, {
          title         = "Campanii (utm_source / utm_medium / utm_campaign)"
          visualization = "table"
          noDataMessage = "Nicio vizită cu parametri utm_* în perioada aleasă."
          query         = <<-KQL
            customEvents
            | where name == "PageView" and isnotempty(customDimensions.utmSource)
            | summarize Entries = count() by Source = tostring(customDimensions.utmSource), Medium = tostring(customDimensions.utmMedium), Campaign = tostring(customDimensions.utmCampaign)
            | order by Entries desc
          KQL
        })
        customWidth = "50"
      },
      {
        type = 3
        name = "languages"
        content = merge(local.workbook_query, {
          title         = "Limba site-ului"
          visualization = "piechart"
          query         = <<-KQL
            customEvents
            | where name == "PageView"
            | summarize Loads = count() by Language = tostring(customDimensions.locale)
          KQL
        })
        customWidth = "50"
      },
      {
        type = 3
        name = "business-events"
        content = merge(local.workbook_query, {
          title         = "Activitate pe zi (conturi, anunțuri, conversații, raportări, vânzări/închirieri)"
          visualization = "timechart"
          query         = <<-KQL
            traces
            | extend Event = tostring(customDimensions.Event)
            | where Event in (${join(", ", [for e in local.workbook_business_events : "\"${e}\""])})
            | where Event == "AccountCreated" or message has " succeeded"
            | summarize Count = count() by bin(timestamp, 1d), Event
          KQL
        })
      },
      {
        type = 3
        name = "refusals"
        content = merge(local.workbook_query, {
          title         = "Refuzuri (validări, autentificări eșuate, limite) — top coduri"
          visualization = "table"
          noDataMessage = "Niciun refuz în perioada aleasă."
          query         = <<-KQL
            traces
            | where isnotempty(customDimensions.Event) and message has " refused: "
            | summarize Count = count() by Event = tostring(customDimensions.Event), Code = tostring(customDimensions.Code)
            | top 20 by Count
          KQL
        })
        customWidth = "50"
      },
      {
        type = 3
        name = "errors"
        content = merge(local.workbook_query, {
          title         = "Erori pe zi, pe aplicație"
          visualization = "barchart"
          noDataMessage = "Nicio eroare în perioada aleasă."
          query         = <<-KQL
            exceptions
            | summarize Errors = count() by bin(timestamp, 1d), App = cloud_RoleName
          KQL
        })
        customWidth = "50"
      },
      {
        type = 3
        name = "response-times"
        content = merge(local.workbook_query, {
          title         = "Timp de răspuns (p95, ms) — web și API"
          visualization = "timechart"
          query         = <<-KQL
            requests
            | where cloud_RoleName in ("imova-web", "imova-api") and name !has "/health"
            | summarize P95 = percentile(duration, 95) by bin(timestamp, 1h), App = cloud_RoleName
          KQL
        })
      },
    ]
    fallbackResourceIds = [lower(azurerm_application_insights.main.id)]
    "$schema"           = "https://github.com/Microsoft/Application-Insights-Workbooks/blob/master/schema/workbook.json"
  }
}

resource "azurerm_application_insights_workbook" "dashboard" {
  # A workbook's name must be a GUID; derived from the environment so it stays the same.
  name                = uuidv5("url", "https://imova.md/workbooks/${local.name}/dashboard")
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  display_name        = "IMOVA — trafic și activitate (${var.environment})"
  source_id           = lower(azurerm_application_insights.main.id)
  category            = "workbook"
  data_json           = jsonencode(local.workbook)

  tags = local.tags
}
