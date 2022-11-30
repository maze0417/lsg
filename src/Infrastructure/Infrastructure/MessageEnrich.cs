using System;
using LSG.Core.Enums;
using LSG.Core.Messages.Events;
using LSG.Core.Messages.Hub;
using LSG.Core.Tokens;
using LSG.SharedKernel.Logger;

namespace LSG.Infrastructure
{
    public interface IMessageEnrich
    {
        T EnrichServerToClientMessage<T>(T response, UserTokenData tokenData, ApiResponseCode code,
            string message = null, string nickName = null) where T : ServerToClientMessage;

        T EnrichInternalEvent<T>(T baseEvent) where T : BaseEvent;
    }

    public sealed class MessageEnrich : IMessageEnrich
    {
        private readonly ILsgLogger _lsgLogger;

        public MessageEnrich(ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;
        }


        T IMessageEnrich.EnrichServerToClientMessage<T>(T response, UserTokenData tokenData, ApiResponseCode code,
            string message, string nickName)

        {
            if (_lsgLogger.CorrelationId == Guid.Empty && response.CorrelationId != Guid.Empty)
            {
                _lsgLogger.CorrelationId = response.CorrelationId;
            }

            if (_lsgLogger.CorrelationId != Guid.Empty && response.CorrelationId == Guid.Empty)
            {
                response.CorrelationId = _lsgLogger.CorrelationId;
            }

            response.Code = code;
            response.Message = message;
            response.Id = tokenData.UserIdentifier;
            response.NickName = nickName ?? tokenData.Name;
            return response;
        }

        T IMessageEnrich.EnrichInternalEvent<T>(T baseEvent)
        {
            if (_lsgLogger.CorrelationId == Guid.Empty && baseEvent.CorrelationId != Guid.Empty)
            {
                _lsgLogger.CorrelationId = baseEvent.CorrelationId;
            }

            if (_lsgLogger.CorrelationId != Guid.Empty && baseEvent.CorrelationId == Guid.Empty)
            {
                baseEvent.CorrelationId = _lsgLogger.CorrelationId;
            }

            return baseEvent;
        }
    }
}