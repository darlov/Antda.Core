using Antda.Messages.Extensions.Microsoft.DependencyInjection.Tests.Handlers;
using Antda.Messages.Extensions.Microsoft.DependencyInjection.Tests.Messages;
using Antda.Messages.Extensions.Microsoft.DependencyInjection.Tests.Middlewares;
using Microsoft.Extensions.DependencyInjection;

namespace Antda.Messages.Extensions.Microsoft.DependencyInjection.Tests;

public class MessagesServiceCollectionExtensionsTests
{
  [Fact]
  public async Task AddAntdaMessages_WithValidAssemblies_ShouldAddTypes()
  {
    var services = new ServiceCollection();

    services.AddAntdaMessages(this.GetType().Assembly)
      .UseHandleMessages();

    await using var provider = services.BuildServiceProvider();

    var sender = provider.GetRequiredService<IMessageSender>();

    var defaultTestMessage = new DefaultMessage("Test");
    var defaultResult = await sender.SendAsync(defaultTestMessage);

    Assert.Equal("Test", defaultResult);
  }
  
  [Fact]
  public async Task AddAntdaMessages_WithMiddleware_ShouldModifyResult()
  {
    const string messageText = "Message text";
    const string additionalText = " Bla Bla";
    
    var services = new ServiceCollection();
    services.AddTransient(_ => new ModifyResultMiddleware(additionalText));

    services.AddAntdaMessages(this.GetType().Assembly)
      .UseHandleMessages()
      .UseMiddleware<ModifyResultMiddleware>();

    await using var provider = services.BuildServiceProvider();
    
    var sender = provider.GetRequiredService<IMessageSender>();

    var defaultTestMessage = new DefaultMessage(messageText);
    var defaultResult = await sender.SendAsync(defaultTestMessage);

    Assert.Equal(messageText + additionalText, defaultResult);
  }
  
  [Fact]
  public async Task AddAntdaMessages_WithHandlers_ShouldResolve()
  {
    var services = new ServiceCollection();

    services.AddAntdaMessagesCore()
      .UseHandleMessages();

    services.AddMessageHandler<DefaultHandler>();
    services.AddMessageHandler<NoResultHandler<NoResultMessage>>();

    await using var provider = services.BuildServiceProvider();

    var sender = provider.GetRequiredService<IMessageSender>();

    var defaultMessage = new DefaultMessage("Test");
    var defaultResult = await sender.SendAsync(defaultMessage);

    Assert.Equal("Test", defaultResult);
    
    await sender.SendAsync<NoResultMessage>();
  }
  
  [Fact]
  public async Task AddAntdaMessages_WitCustomMiddleware_ShouldResolve()
  {
    const string customPropText = "Custom text";
    
    var services = new ServiceCollection();

    services.AddTransient(_ => new CustomTypeMiddleware(customPropText));
    services.AddMessageHandler<DefaultHandler>();
    services.AddMessageHandler<NoResultHandler<NoResultMessage>>();
    services.AddMessageHandler<NoResultHandler<NoResultWithCustomMessage>>();
      
    services.AddAntdaMessagesCore()
      .UseMiddleware<CustomTypeMiddleware>()
      .UseHandleMessages();

    await using var provider = services.BuildServiceProvider();

    var sender = provider.GetRequiredService<IMessageSender>();

    var defaultMessage = new DefaultMessage("Test");

    _ = await sender.SendAsync(defaultMessage);

    Assert.Equal(customPropText, defaultMessage.CustomProp);
    
    var noResultWithCustomMessage = new NoResultWithCustomMessage();
    await sender.SendAsync(noResultWithCustomMessage);
    
    Assert.Equal(customPropText, noResultWithCustomMessage.CustomProp);
    
    var noResultMessage = new NoResultMessage();
    await sender.SendAsync(noResultMessage);
  }
}