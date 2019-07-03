//-----------------------------------------------------------------------------
// FILE:        MessageCenter.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2015-2016 by Neon Research, LLC.  All rights reserved.
// LICENSE:     MIT License: https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Neon.Stack.XamarinExtensions
{
    /// <summary>
    /// Wraps the Xamarin.Forms <see cref="MessagingCenter"/> class with easier-to-use 
    /// methods and global message types.
    /// </summary>
    public class MessageCenter
    {
        //---------------------------------------------------------------------
        // Common messages

        /// <summary>
        /// This used for broadcasting the application's busy status between UX
        /// components.  This message will typically be broadcast by the application's
        /// view-model when the busy state changes.
        /// </summary>
        public const string IsBusyMessage = "global::IsBusy";

        //---------------------------------------------------------------------
        // Static members

        private static readonly MessageCenter instance = new MessageCenter();

        /// <summary>
        /// Subscribes an object to a simple message.
        /// </summary>
        /// <param name="subscriber">The subscriber instance.</param>
        /// <param name="message">The message text.</param>
        /// <param name="action">The action to be performed when a message is received.</param>
        public static void Subscribe(object subscriber, string message, Action action)
        {
            var wrappedAction = new Action<MessageCenter>(
                sender =>
                {
                    action();
                });

            MessagingCenter.Subscribe<MessageCenter>(subscriber, message, wrappedAction);
        }

        /// <summary>
        /// Subscribes an object to a message with a parameter of a specific type.
        /// </summary>
        /// <typeparam name="TArg">The message argument type.</typeparam>
        /// <param name="subscriber">The subscriber instance.</param>
        /// <param name="message">The message text.</param>
        /// <param name="action">The action to be performed when a message is received.</param>
        public static void Subscribe<TArg>(object subscriber, string message, Action<TArg> action)
        {
            var wrappedAction = new Action<MessageCenter, TArg>(
                (sander, arg) =>
                {
                    action(arg);
                });

            MessagingCenter.Subscribe<MessageCenter, TArg>(subscriber, message, wrappedAction);
        }

        /// <summary>
        /// Unsubscribes an object to a simple message.
        /// </summary>
        /// <param name="subscriber">The subscriber instance.</param>
        /// <param name="message">The message text.</param>
        public static void Unsubscribe(object subscriber, string message)
        {
            MessagingCenter.Unsubscribe<MessageCenter>(subscriber, message);
        }

        /// <summary>
        /// Unsbscribes an object to a message with a parameter of a specific type.
        /// </summary>
        /// <typeparam name="TArg">The message argument type.</typeparam>
        /// <param name="subscriber">The subscriber instance.</param>
        /// <param name="message">The message text.</param>
        public static void Unsubscribe<TArg>(object subscriber, string message)
        {
            MessagingCenter.Unsubscribe<MessageCenter, TArg>(subscriber, message);
        }

        /// <summary>
        /// Broadcasts a message to all subscribed listeners.
        /// </summary>
        /// <param name="message">The message text.</param>
        public static void Broadcast(string message)
        {
            MessagingCenter.Send<MessageCenter>(instance, message);
        }

        /// <summary>
        /// Broadcasts a message with an argument to all subscribed listeners.
        /// </summary>
        /// <typeparam name="TArg">The message argument type.</typeparam>
        /// <param name="message">The message text.</param>
        /// <param name="arg">The argument value.</param>
        public static void Broadcast<TArg>(string message, TArg arg)
        {
            MessagingCenter.Send<MessageCenter, TArg>(instance, message, arg);
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Private constructor.
        /// </summary>
        private MessageCenter()
        {
        }
    }
}
