dependencies
| where timestamp > ago(30m)
| where cloud_RoleName == "SeniorDeveloperConsole"
| summarize count() by bin(timestamp, 1m), name, resultCode
| order by timestamp desc
