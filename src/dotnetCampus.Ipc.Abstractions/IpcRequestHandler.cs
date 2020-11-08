﻿namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 用于在服务器端处理客户端请求的处理器
    /// </summary>
    public interface IpcRequestHandler
    {
        /// <summary>
        /// 处理客户端发过来的请求
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        IpcRequestMessage HandleRequestMessage(IIpcRequestContext requestContext);
    }
}