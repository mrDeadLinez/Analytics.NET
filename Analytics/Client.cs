﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Analytics.Exception;
using Analytics.Model;
using Analytics.Request;
using Analytics.Trigger;
using Analytics.Stats;

namespace Analytics
{
    /// <summary>
    /// A Segment.io REST client
    /// </summary>
    public class Client
    {
        private IRequestHandler requestHandler;
        private string secret;
        private Options options;

        public Statistics Statistics { get; set; }

        #region Events

        public delegate void FailedHandler(BaseAction action, System.Exception e);
        public delegate void SucceededHandler(BaseAction action);

        public event FailedHandler Failed;
        public event SucceededHandler Succeeded;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new REST client with a specified API secret and default options
        /// </summary>
        /// <param name="secret"></param>
        public Client(string secret) : this(secret, new Options()) {}

        /// <summary>
        /// Creates a new REST client with a specified API secret and default options
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="options"></param>
        public Client(string secret, Options options)
        {
            if (String.IsNullOrEmpty(secret))
                throw new InvalidOperationException("Please supply a valid secret to initialize.");

            this.Statistics = new Statistics();
            this.secret = secret;
            this.options = options;

            requestHandler = new BatchingRequestHandler(new IFlushTrigger[] {
                new FlushAtTrigger(options.FlushAt),
                new FlushAfterTrigger(options.FlushAfter)
            });

            requestHandler.Initialize(this, secret);
        }

        #endregion

        #region Properties

        public string Secret
        {
            get
            {
                return secret;
            }
        }


        public Options Options
        {
            get
            {
                return options;
            }
        }

        #endregion

        #region Utils

        private void clean(ApiDictionary properties)
        {
            if (properties != null)
            {
                List<string> toRemove = new List<string>();

                foreach (var pair in properties)
                {
                    if (pair.Value is string || pair.Value is bool || IsNumeric(pair.Value))
                    {
                        // good case
                    }
                    else if (pair.Value is DateTime)
                    {
                        // this is good
                    }
                    else
                    {
                        // remove this parameter
                        toRemove.Add(pair.Key);
                    }
                }

                foreach (string removal in toRemove) properties.Remove(removal);
            }
        }

        private static bool IsNumeric(object expression)
        {
            if (expression == null)
                return false;

            double number;
            return Double.TryParse(Convert.ToString(expression, CultureInfo.InvariantCulture), 
                System.Globalization.NumberStyles.Any, NumberFormatInfo.InvariantInfo, out number);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Identifying a visitor ties all of their actions to an ID you 
        /// recognize and records visitor traits you can segment by.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity.</param>
        /// 
        public void Identify(string sessionId, string userId)
        {
            Identify(sessionId, userId, null, null, null);
        }

        /// <summary>
        /// Identifying a visitor ties all of their actions to an ID you 
        /// recognize and records visitor traits you can segment by.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity.</param>
        /// 
        /// <param name="traits">A dictionary with keys like “Subscription Plan” or 
        /// “Favorite Genre”. You can segment your users by any trait you record. 
        /// Pass in values in key-value format. String key, then its value 
        /// { String, Integer, Boolean, Double, or Date are acceptable types for a value. } 
        /// So, traits array could be: "Subscription Plan", "Premium", 
        /// "Friend Count", 13 , and so forth.  </param>
        public void Identify(string sessionId, string userId, Traits traits)
        {
            Identify(sessionId, userId, traits, null, null);
        }

        /// <summary>
        /// Identifying a visitor ties all of their actions to an ID you 
        /// recognize and records visitor traits you can segment by.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity.</param>
        /// 
        /// <param name="traits">A dictionary with keys like “Subscription Plan” or 
        /// “Favorite Genre”. You can segment your users by any trait you record. 
        /// Pass in values in key-value format. String key, then its value 
        /// { String, Integer, Boolean, Double, or Date are acceptable types for a value. } 
        /// So, traits array could be: "Subscription Plan", "Premium", 
        /// "Friend Count", 13 , and so forth.  </param>
        /// 
        /// <param name="context"> A dictionary with additional information thats related to the visit. 
        /// Examples are userAgent, and IP address of the visitor. 
        /// Feel free to pass in null if you don't have this information.</param>
        /// 
        public void Identify(string sessionId, string userId, Traits traits, Context context)
        {
            Identify(sessionId, userId, traits, context, null);
        }

        /// <summary>
        /// Identifying a visitor ties all of their actions to an ID you 
        /// recognize and records visitor traits you can segment by.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity.</param>
        /// 
        /// <param name="traits">A dictionary with keys like “Subscription Plan” or 
        /// “Favorite Genre”. You can segment your users by any trait you record. 
        /// Pass in values in key-value format. String key, then its value 
        /// { String, Integer, Boolean, Double, or Date are acceptable types for a value. } 
        /// So, traits array could be: "Subscription Plan", "Premium", 
        /// "Friend Count", 13 , and so forth.  </param>
        /// 
        /// <param name="context"> A dictionary with additional information thats related to the visit. 
        /// Examples are userAgent, and IP address of the visitor. 
        /// Feel free to pass in null if you don't have this information.</param>
        /// 
        /// <param name="timestamp">  If this event happened in the past, the timestamp 
        /// can be used to designate when the identification happened. Careful with this one,
        /// if it just happened, leave it null.</param>
        /// 
        /// 
        public void Identify(string sessionId, string userId, Traits traits, 
            Context context, DateTime? timestamp)
        {

            if (String.IsNullOrEmpty(sessionId) && String.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Please supply either a valid sessionId or userId (or both) to Identify.");

            clean(traits);

            Identify identify = new Identify(sessionId, userId, traits, context, timestamp);
            
            requestHandler.Process(identify);
        }

        /// <summary>
        /// Whenever a user triggers an event on your site, you’ll want to track it 
        /// so that you can analyze and segment by those events later.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity. 
        /// This makes it possible for you to run things like segment-based email campaigns.</param>
        /// 
        /// <param name="eventName">The event name you are tracking. It is recommended 
        /// that it is in human readable form. For example, "Bought T-Shirt" 
        /// or "Started an exercise"</param>
        /// 
        public void Track(string sessionId, string userId, string eventName)
        {
            Track(sessionId, userId, eventName, null, null, null);
        }

        /// <summary>
        /// Whenever a user triggers an event on your site, you’ll want to track it.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity. 
        /// This makes it possible for you to run things like segment-based email campaigns.</param>
        /// 
        /// <param name="eventName">The event name you are tracking. It is recommended 
        /// that it is in human readable form. For example, "Bought T-Shirt" 
        /// or "Started an exercise"</param>
        /// 
        /// <param name="properties"> A dictionary with items that describe the event 
        /// in more detail. This argument is optional, but highly recommended — 
        /// you’ll find these properties extremely useful later.</param>
        /// 
        public void Track(string sessionId, string userId, string eventName, Properties properties)
        {
            Track(sessionId, userId, eventName, properties, null, null);
        }

        
        /// <summary>
        /// Whenever a user triggers an event on your site, you’ll want to track it 
        /// so that you can analyze and segment by those events later.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity. 
        /// This makes it possible for you to run things like segment-based email campaigns.</param>
        /// 
        /// <param name="eventName">The event name you are tracking. It is recommended 
        /// that it is in human readable form. For example, "Bought T-Shirt" 
        /// or "Started an exercise"</param>
        /// 
        /// <param name="properties"> A dictionary with items that describe the event 
        /// in more detail. This argument is optional, but highly recommended — 
        /// you’ll find these properties extremely useful later.</param>
        /// 
        /// <param name="context"> A dictionary with additional information thats related to the visit. 
        /// Examples are userAgent, and IP address of the visitor. 
        /// Feel free to pass in null if you don't have this information.</param>
        /// 
        /// <param name="timestamp">  If this event happened in the past, the timestamp 
        /// can be used to designate when the identification happened. Careful with this one,
        /// if it just happened, leave it null.</param>
        /// 
        public void Track(string sessionId, string userId, string eventName, Properties properties,
           DateTime? timestamp)
        {
            Track(sessionId, userId, eventName, properties, null, timestamp);
        }

        /// <summary>
        /// Whenever a user triggers an event on your site, you’ll want to track it 
        /// so that you can analyze and segment by those events later.
        /// </summary>
        /// 
        /// <param name="sessionId">The visitor's anonymous identifier until they log in, 
        /// or until your system knows who they are. In web systems, 
        /// this is usually the ID of this user in the sessions table</param>
        /// 
        /// <param name="userId">The visitor's identifier after they log in, or you know 
        /// who they are. This is usually an email, but any unique ID will work. By 
        /// explicitly identifying a user, you tie all of their actions to their identity. 
        /// This makes it possible for you to run things like segment-based email campaigns.</param>
        /// 
        /// <param name="eventName">The event name you are tracking. It is recommended 
        /// that it is in human readable form. For example, "Bought T-Shirt" 
        /// or "Started an exercise"</param>
        /// 
        /// <param name="properties"> A dictionary with items that describe the event 
        /// in more detail. This argument is optional, but highly recommended — 
        /// you’ll find these properties extremely useful later.</param>
        /// 
        /// <param name="context"> A dictionary with additional information thats related to the visit. 
        /// Examples are userAgent, and IP address of the visitor. 
        /// Feel free to pass in null if you don't have this information.</param>
        /// 
        /// <param name="timestamp">  If this event happened in the past, the timestamp 
        /// can be used to designate when the identification happened. Careful with this one,
        /// if it just happened, leave it null.</param>
        /// 
        public void Track(string sessionId, string userId, string eventName, Properties properties, 
            Context context, DateTime? timestamp)
        {
            if (String.IsNullOrEmpty(sessionId) && String.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Please supply either a valid sessionId or userId (or both) to Track.");

            if (String.IsNullOrEmpty(eventName))
                throw new InvalidOperationException("Please supply a valid eventName to Track.");

            clean(properties);

            Track track = new Track(sessionId, userId, eventName, properties, context, timestamp);

            requestHandler.Process(track);
        }

        #endregion

        #region Flush

        /// <summary>
        /// Triggers a queue flush
        /// </summary>
        public void Flush()
        {
            requestHandler.Flush();
        }

        #endregion

        #region Event API

        internal void RaiseSuccess(BaseAction action)
        {
            if (Succeeded != null) Succeeded(action);
        }

        internal void RaiseFailure(BaseAction action, System.Exception e)
        {
            if (Failed != null) Failed(action, e);
        }

        #endregion
    }
}