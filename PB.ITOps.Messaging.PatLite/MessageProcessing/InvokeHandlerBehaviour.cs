using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.MessageMapping;

namespace PB.ITOps.Messaging.PatLite.MessageProcessing
{
    /// <summary>
    /// - Completes message on handler success
    /// - No action on message failure: allow peek lock to expire, and default delivery count for dead lettering
    /// </summary>
    public class InvokeHandlerBehaviour : IMessageProcessingBehaviour
    {
        private readonly IMessageDeserialiser _messageDeserialiser;

        public InvokeHandlerBehaviour(IMessageDeserialiser messageDeserialiser)
        {
            _messageDeserialiser = messageDeserialiser;
        }

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            var message = messageContext.Message;
            var messageBody = await GetMessageBody(message);

            var messageTypeString = message.UserProperties["MessageType"].ToString();
            var handlerForMessageType = MessageMapper.GetHandlerForMessageType(messageTypeString);
            var messageHandler = messageContext.DependencyScope.GetService(handlerForMessageType.HandlerType);
            var typedMessage = _messageDeserialiser.DeserialiseObject(messageBody, handlerForMessageType.MessageType);

            await (Task)handlerForMessageType.HandlerMethod.Invoke(messageHandler, new[] { typedMessage });
        }


        private static async Task<string> GetMessageBody(Message message)
        {
            /*
             * This is a work around to cope with two clients, one client 
             * is publishing with a stream payload, the other is with a 
             * string payload. Once all versions of the PB.ITOps.Messaging.PatSender 
             * are on 2.0.33 or above this can be removed.
             * 
             * Yes, this is awful.
             */
            try
            {
                return MessageAsStringReaderStrategy(message.Clone());
            }
            catch (SerializationException)
            {
                try
                {
                    return await MessageAsStreamReaderStrategy(message.Clone());
                }
                catch (SerializationException)
                {
                    return MessageAsUtf8ByteArrayReaderStrategy(message.Clone());
                }
            }
        }

        private static string MessageAsStringReaderStrategy(Message message)
        {
            return message.GetBody<string>();
        }

        private static async Task<string> MessageAsStreamReaderStrategy(Message message)
        {
            using (var messageStream = message.GetBody<Stream>())
            {
                using (var reader = new StreamReader(messageStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private static string MessageAsUtf8ByteArrayReaderStrategy(Message message)
        {
            return Encoding.UTF8.GetString(message.Body);
        }
    }
}