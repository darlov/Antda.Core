﻿using System.Reflection;
using Antda.Core.Exceptions;
using Antda.Core.Extensions;
using Antda.Core.Helpers;
using Antda.Messages.Internal;
using Antda.Messages.Middleware;

namespace Antda.Messages.DependencyInjection;

public static class ServiceRegistrarExtensions
{
    public static IMiddlewareBuilder AddAntdaMessagesCore<TServiceResolver>(this IServiceRegistrar serviceRegistrar)
        where TServiceResolver : IServiceResolver
    {
        Throw.If.ArgumentNull(serviceRegistrar);

        serviceRegistrar.TryAddSingleton<IServiceResolver, TServiceResolver>();
        serviceRegistrar.TryAddTransient<IMessageSender, MessageSender>();
        serviceRegistrar.TryAddSingleton<IMessageProcessorFactory, MessageProcessorFactory>();
        serviceRegistrar.TryAddSingleton(typeof(IMessageProcessor<,>), typeof(MessageProcessor<,>));
        serviceRegistrar.TryAddSingleton(typeof(IMemoryCacheProvider<>), typeof(MemoryCacheProvider<>));

        var middlewareBuilder = new MiddlewareBuilder();
        serviceRegistrar.AddSingleton<IMiddlewareProvider>(middlewareBuilder);

        serviceRegistrar.TryAddTransient(typeof(HandleMessageMiddleware<,>));

        return middlewareBuilder;
    }
    
    public static IMiddlewareBuilder AddAntdaMessages<TServiceResolver>(this IServiceRegistrar services, params Assembly[] assembliesToScan)
        where TServiceResolver : IServiceResolver
    {
        return services.AddAntdaMessages<TServiceResolver>((IEnumerable<Assembly>)assembliesToScan);
    }

    public static IMiddlewareBuilder AddAntdaMessages<TServiceResolver>(this IServiceRegistrar serviceRegistrar, IEnumerable<Assembly> assembliesToScan)
        where TServiceResolver : IServiceResolver
    {
        var builder = serviceRegistrar.AddAntdaMessagesCore<TServiceResolver>();
    
        foreach (var typeInfo in TypeHelper.FindAllowedTypes(assembliesToScan))
        {
            serviceRegistrar.AddMessageHandlerInternal(typeInfo, true);
        }

        return builder;
    }
    
    public static IServiceRegistrar AddMessageHandler<T>(this IServiceRegistrar serviceRegistrar) 
        => serviceRegistrar.AddMessageHandler(typeof(T));
    
    public static IServiceRegistrar AddMessageHandler(this IServiceRegistrar serviceRegistrar, Type handlerType)
        => serviceRegistrar.AddMessageHandlerInternal(handlerType, false);
  
    private static IServiceRegistrar AddMessageHandlerInternal(this IServiceRegistrar serviceRegistrar, Type handlerType, bool skipNotSupported)
    {
        Throw.If.ArgumentNull(serviceRegistrar);
        Throw.If.ArgumentNull(handlerType);

        var types = TypeHelper.FindTypes(handlerType, typeof(IMessageHandler<,>)).ToList();
        if (!types.Any())
        {
            if (skipNotSupported)
            {
                return serviceRegistrar;
            }
      
            throw new NotSupportedException("Message handler should implemented IMessageHandler<in TMessage, TResult> interface");
        }

        if (handlerType.IsOpenGeneric())
        {
            throw new NotSupportedException($"The open generic handler is not supported for {handlerType}");
        }
    
        foreach (var interfaceType in types)
        {
            serviceRegistrar.AddTransient(interfaceType, handlerType);
        }

        return serviceRegistrar;
    }
    

}