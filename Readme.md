Setup local development 
===

Prerequisite 
---
- Install docker via  https://hub.docker.com/editions/community/docker-ce-desktop-windows/
- Switch directory to solution , for example
- Install Dotnet core 7  https://dotnet.microsoft.com/download/dotnet-core
- If you are Win 10 Home edition user, follow the steps to install docker from https://blog.cwlove.idv.tw/win10-home-install-hyper-v-docker/

``` 
cd C:\Users\Maze\RiderProjects\LSG
```

Run Infrastructure container  
---
- Run Infra container like Sql Server by docker 
``` 
docker compose up  -d 
```

Run Lsg Projects
---
- Run web site by docker
``` 
docker compose up  -d 
```
- If you put this project in d:\ , and error occurs with message "File sharing ...",
you may consider to setup docker->setting->file sharing -> select d: manually  
- Verify http://localhost:8001/api/status canConnectToDb should be true
- Verify the Database with some tools (like ssms or sqlcmd) 
to check if created with port 14333 and credentials that in Web/LsgApi/appsettings.json 


Shutdown Lsg Projects
---
- shutdown container via command belows:
``` 
docker-compose down
```

Create database and just launch api
---
- This command will run lsgapi only to create and seed database 

**That could be refer in docker-compose.yml service name,Sample as belows** 
``` 
docker-compose up  -d --build lsgapi
```


Check Site running
---
- This command will run lsgapi only to create and seed database

**That could be refer in docker-compose.yml service name,Sample as belows**
``` 
#lsg api
curl http://localhost:8001/api/status?key=4e5461aa-5e68-411c-8395-fecb65460825

#lsg frontend
curl http://localhost:8002/api/status?key=4e5461aa-5e68-411c-8395-fecb65460825

#lsg logger
curl http://localhost:8003/api/status?key=4e5461aa-5e68-411c-8395-fecb65460825

```
Response
```json
{
    "Site": "LsgApi",
    "PhysicalPath": "D:\\Projects\\lsg\\src\\Hosts\\GenericHost\\bin\\Debug\\net7.0\\",
    "MachineName": "DESKTOP-456UBOB",
    "Version": "1.16.0.0",
    "EnvironmentVariables": {
        "ASPNETCORE_SERVERNAME": null,
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES": null
    },
    "ServerInfos": [
        {
            "ServerType": "Redis",
            "Host": "host.docker.internal,channelPrefix=core,abortConnect=false",
            "IsConnected": true
        },
        {
            "ServerType": "Nats",
            "Host": "nats://host.docker.internal:4222",
            "IsConnected": true
        },
        {
            "ServerType": "Database",
            "Host": "host.docker.internal,14333",
            "IsConnected": true,
            "Message": "ok",
            "Name": "Lsg"
        }
    ]
}
```


Api document 
---
- Just available in development url :
http://localhost:8002/doc

