How to implement schema changes to database
===

Step1 
---
- dotnet ef must be installed as a global or local tool. Most developers will install dotnet ef as a global tool with the following command:
``` 
dotnet tool install --global dotnet-ef
```
 
Step2
---
- Add Migration
```
dotnet ef migrations add Initial --project "src/Infrastructure/DataServices" --startup-project "src/Hosts/GenericHost" -c LsgRepository

```

Step3
---
- Update Database To Latest
```
dotnet ef database update --project "src/Infrastructure/DataServices" --startup-project "src/Hosts/GenericHost" -c LsgRepository
```

Misc commands
===

Drop Db
---
```
dotnet ef database drop --project "src/Infrastructure/DataServices" --startup-project "src/Hosts/GenericHost" -c LsgRepository --force
```

dbcontext info
---
```
dotnet ef dbcontext info --project "src/Infrastructure/DataServices" --startup-project "src/Hosts/GenericHost" -c LsgRepository
```

Get Script
---
```
dotnet ef migrations script --project "src/Infrastructure/DataServices" --startup-project "src/Hosts/GenericHost" -c LsgRepository -o init.sql
```
