﻿//-----------------------------------------------------------------------------
// FILE:	    ILog.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neon.Stack.Diagnostics
{
    /// <summary>
    /// Defines the methods and properties for a diagnostics logger. 
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Returns <c>true</c> if <b>debug</b> logging is enabled.
        /// </summary>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Returns <c>true</c> if <b>info</b> logging is enabled.
        /// </summary>
        bool IsInfoEnabled { get; }

        /// <summary>
        /// Returns <c>true</c> if <b>warn</b> logging is enabled.
        /// </summary>
        bool IsWarnEnabled { get; }

        /// <summary>
        /// Returns <c>true</c> if <b>error</b> logging is enabled.
        /// </summary>
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Returns <c>true</c> if <b>fatal</b> logging is enabled.
        /// </summary>
        bool IsFatalEnabled { get; }

        /// <summary>
        /// Logs a <b>debug</b> message.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Debug(object message, string activityId = null);

        /// <summary>
        /// Logs an <b>info</b> message.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Info(object message, string activityId = null);

        /// <summary>
        /// Logs a <b>warn</b> message.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Warn(object message, string activityId = null);

        /// <summary>
        /// Logs an <b>error</b> message.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Error(object message, string activityId = null);

        /// <summary>
        /// Logs a <b>fatal</b> message.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Fatal(object message, string activityId = null);

        /// <summary>
        /// Logs a <b>debug</b> message along with exception information.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="e">The exception.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Debug(object message, Exception e, string activityId = null);

        /// <summary>
        /// Logs an <b>info</b> message along with exception information.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="e">The exception.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Info(object message, Exception e, string activityId = null);

        /// <summary>
        /// Logs a <b>warn</b> message along with exception information.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="e">The exception.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Warn(object message, Exception e, string activityId = null);

        /// <summary>
        /// Logs an <b>error</b> message along with exception information.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="e">The exception.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Error(object message, Exception e, string activityId = null);

        /// <summary>
        /// Logs a <b>fatal</b> message along with exception information.
        /// </summary>
        /// <param name="message">The object that will be serialized into the message.</param>
        /// <param name="e">The exception.</param>
        /// <param name="activityId">The optional activity ID.</param>
        void Fatal(object message, Exception e, string activityId = null);
    }
}
