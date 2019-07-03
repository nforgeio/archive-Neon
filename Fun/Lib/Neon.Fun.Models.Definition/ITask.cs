//-----------------------------------------------------------------------------
// FILE:	    ITask.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;

using Neon.Stack.Common;
using Neon.Stack.Data;

namespace Neon.Fun.Models
{
    /// <summary>
    /// Describes a base task.
    /// </summary>
    [Entity(Type = EntityTypes.Task, Namespace = FunConst.Namespace)]
    public interface ITask
    {
        /// <summary>
        /// Returns the entity type.
        /// </summary>
        [EntityProperty(IsTypeProperty = true)]
        EntityTypes EntityType { get; }

        /// <summary>
        /// The brief task summary.
        /// </summary>
        [EntityProperty(Name = "summary")]
        string Summary { get; set; }

        /// <summary>
        /// The more detailed task description.
        /// </summary>
        [EntityProperty(Name = "description")]
        string Description { get; set; }

        /// <summary>
        /// The task due date (UTC).
        /// </summary>
        [EntityProperty(Name = "due")]
        DateTime DueDate { get; set; }

        /// <summary>
        /// Links to the accounts assigned the tasks.
        /// </summary>
        [EntityProperty(Name = "assignees", IsLink = true)]
        IAccount[] Assignees { get; set; }
    }
}
