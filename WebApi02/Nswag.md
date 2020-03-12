> Asp.net core 3.1   
> Nuget:Nswag.AspNetCore

1. 注册Swagger服务,startup.cs--ConfigureServices
```
//Nswag一：注册swagger服务
            services.AddSwaggerDocument(config =>
            {
                config.PostProcess = document =>
                {
                    document.Info.Version = "v1.0.1";
                    document.Info.Title = "天气Api";
                    document.Info.Description = "这是一个.Net Core Web Api";
                    document.Info.TermsOfService = "http://www.google.com";
                    document.Info.Contact = new NSwag.OpenApiContact
                    {
                        Name = "张三",
                        Email = "a@b.com",
                        Url = "http://www.abc.com"
                    };
                    document.Info.License = new NSwag.OpenApiLicense
                    {
                        Name = "许可证名称",
                        Url = "http://www.d.com"
                    };
                };
            });
```

2. 启用中间件为生成的 Swagger 规范和 Swagger UI 提供服务,startup.cs -- Configure
```
//Nswag二：启用中间件为生成的 Swagger 规范和 Swagger UI 提供服务：
            app.UseOpenApi();
            app.UseSwaggerUi3();
```

3. 启用XML注释： projectname.csproj
```
  <PropertyGroup>
    <!--Nswag三：启用XML注释-->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>
```

