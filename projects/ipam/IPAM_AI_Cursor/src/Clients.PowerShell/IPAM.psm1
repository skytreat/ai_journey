function Get-IpamAddressSpaces { param([string] = http://localhost:5080) Invoke-RestMethod -Method Get -Uri /api/v1/address-spaces }
