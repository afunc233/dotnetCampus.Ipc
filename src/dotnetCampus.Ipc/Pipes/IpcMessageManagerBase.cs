﻿using System;
using System.IO;

using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Messages;
using dotnetCampus.Ipc.Utils.Extensions;

namespace dotnetCampus.Ipc.Pipes
{
    class IpcMessageManagerBase
    {
        // 为什么将请求和响应的消息封装都放在一个类里面？这是为了方便更改，和调试
        // 如果放在两个类或两个文件里面，也许就不好调试对应关系
        protected static IpcBufferMessageContext CreateResponseMessageInner(IpcClientRequestMessageId messageId, in IpcMessage response)
        {
            /*
           * MessageHeader
           * MessageId
            * Response Message Length
           * Response Message
           */
            var currentMessageIdByteList = BitConverter.GetBytes(messageId.MessageIdValue);

            var responseMessageLengthByteList = BitConverter.GetBytes(response.Body.Length);
            return new IpcBufferMessageContext
            (
                response.Tag,
                IpcMessageCommandType.ResponseMessage | response.CoreMessageType.AsMessageCommandTypeFlags(),
                new IpcMessageBody(ResponseMessageHeader),
                new IpcMessageBody(currentMessageIdByteList),
                new IpcMessageBody(responseMessageLengthByteList),
                response.Body
            );
        }

        protected static IpcBufferMessageContext CreateRequestMessageInner(in IpcMessage request, ulong currentMessageId)
        {
            /*
            * MessageHeader
            * MessageId
            * Request Message Length
            * Request Message
            */
            var currentMessageIdByteList = BitConverter.GetBytes(currentMessageId);

            var requestMessageLengthByteList = BitConverter.GetBytes(request.Body.Length);

            return new IpcBufferMessageContext
            (
                request.Tag,
                IpcMessageCommandType.RequestMessage | request.CoreMessageType.AsMessageCommandTypeFlags(),
                new IpcMessageBody(RequestMessageHeader),
                new IpcMessageBody(currentMessageIdByteList),
                new IpcMessageBody(requestMessageLengthByteList),
                request.Body
            );
        }

        private static bool CheckHeader(Stream stream, byte[] header)
        {
            for (var i = 0; i < header.Length; i++)
            {
                if (stream.ReadByte() == header[i])
                {
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        protected static bool CheckResponseHeader(Stream stream)
        {
            var header = ResponseMessageHeader;

            return CheckHeader(stream, header);
        }
        protected static bool CheckRequestHeader(Stream stream)
        {
            var header = RequestMessageHeader;
            return CheckHeader(stream, header);
        }


        /// <summary>
        /// 用于标识请求消息
        /// </summary>
        /// 0x52, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74 0x00 就是 Request 字符
        protected static byte[] RequestMessageHeader { get; } = { 0x52, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74, 0x00 };

        protected static byte[] ResponseMessageHeader { get; } = { 0x52, 0x65, 0x73, 0x70, 0x6F, 0x6E, 0x73, 0x65 };
    }
}
