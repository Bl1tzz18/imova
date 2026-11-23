container_registry_name = "acrimova"
dns_zone_name           = "imova.md"

# contact@imova.md: the Zoho Mail mailbox (infra/README.md, "Email").
apex_txt_extra = ["zoho-verification=zb44528895.zmverify.zoho.eu"]
spf_includes   = ["zoho.eu"]
mx_records = {
  "10" = "mx.zoho.eu"
  "20" = "mx2.zoho.eu"
  "50" = "mx3.zoho.eu"
}
txt_records = {
  # Zoho's DKIM key (public): mail sent from Zoho is signed with it.
  "zmail._domainkey" = "v=DKIM1; k=rsa; p=MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCrXdrVwTTCSHnPAcywzMPoLYwIVX3rAgLwKJP4drVpM5gHHmjmbjObWOxr2V9zZpdtzVeE0KJxWxPlK7QJ6L006alvDu73mDESVREVfqaO+wEtf1u0Gd4ArSo367qOAl2nRG8DNtLVigU3jbWwgrwsN4NSLLupVCd/GJzE+KvE7wIDAQAB"
}
