using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Infrastructure;
using LSG.Infrastructure.Filters;
using LSG.Infrastructure.HostServices;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable AsyncConverter.AsyncMethodNamingHighlighting


namespace LSG.IntegrationTests.Hosts;

public abstract class HostBase<T> : Base<T> where T : BaseStartup
{
    protected IHttpClientFactory HttpClientFactory;
    protected ILsgConfig LsgConfig;


    public override async Task Init()
    {
        await base.Init();
        HttpClientFactory = DefaultFactory.GetRequiredService<IHttpClientFactory>();
        LsgConfig = DefaultFactory.GetRequiredService<ILsgConfig>();
    }

    public abstract Task CanGetAllRegisteredTypes();

    public abstract Task CanGetRootStatus();


    public abstract Task CanGetApiStatus();

    public abstract Task CanGetHealth();


    protected void ResolveRegistrationsAndControllers()
    {
        ResolveRegistrations();
        ResolveControllers();

        Assert.Pass("no error");
    }

    private void ResolveRegistrations()
    {
        var registrations = DefaultFactory.GetRequiredService<IServiceCollection>();
        registrations.Count.Should().BeGreaterThan(10, "should have more registered service");
        foreach (var registration in registrations)
        {
            if (registration.ImplementationType?.IsGenericTypeDefinition ?? false) continue;


            var implementationType =
                registration.ImplementationType ?? registration.ImplementationInstance?.GetType();


            if (Ignored.Namespaces.Any(i => implementationType?.Namespace?.StartsWith(i) ?? false)) continue;

            if (!registration.ImplementationType?.IsClass ?? false) continue;

            try
            {
                var instance = DefaultFactory.GetRequiredService(registration.ServiceType);
                if (instance.GetType().ToString().StartsWith("LSG"))
                    Console.WriteLine($@"Resolved {instance.GetType()}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Assert.Fail($"Error when resolving {registration.ServiceType.Name}.");
            }


            //Check that interface of factory (Func<InterfaceName>) is registered in container
            implementationType?
                .GenericTypeArguments
                .Where(type => type.IsInterface)
                .ForEach(type =>
                {
                    if (registrations.All(r => r.ServiceType != type))
                        throw new Exception($"Error when resolving {type.Name}.");
                });
        }
    }

    private void ResolveControllers()
    {
        var controllerType = GetInstanceTypesByBase(typeof(T));
        var activator = DefaultFactory.GetRequiredService<IControllerActivator>();

        var errors = new Dictionary<Type, Exception>();
        foreach (var type in controllerType)
            try
            {
                var actionContext = new ActionContext(
                    new DefaultHttpContext
                    {
                        RequestServices = DefaultFactory
                    },
                    new RouteData(),
                    new ControllerActionDescriptor
                    {
                        ControllerTypeInfo = type.GetTypeInfo()
                    });

                activator.Create(new ControllerContext(actionContext));
                Console.WriteLine($@"Resolving {type.Name} Success.");
            }
            catch (Exception e)
            {
                errors.Add(type, e);
            }

        if (errors.Any())
            Assert.Fail(
                string.Join(
                    Environment.NewLine,
                    errors.Select(x => $"Failed to resolve controller {x.Key.Name} due to {x.Value}")));
    }


    private Type[] GetInstanceTypesByBase(Type type)
    {
        var assembly = Assembly.GetAssembly(type);
        return assembly?
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .Where(t => t.IsPublic && !t.IsAbstract)
            .Where(t =>
            {
                var accept = (RouteAcceptWhenSiteIsAttribute)t.GetCustomAttribute(
                    typeof(RouteAcceptWhenSiteIsAttribute));

                if (accept != null)
                {
                    if (accept.Sites.Any(a => a == CurrentSite))
                        return true;
                    return false;
                }

                throw new InvalidOperationException(
                    $"{t.FullName} didn't have attr RouteAccept attr");
            })
            .ToArray();
    }
}

public class Ignored
{
    public static string[] Namespaces = { "System." };
}