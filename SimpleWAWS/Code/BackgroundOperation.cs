﻿using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace SimpleWAWS.Code
{
    public abstract class BackgroundOperation
    {
        [JsonIgnore]
        public Guid OperationId { get; set; }

        [JsonIgnore]
        public OperationType Type { get; set; }

        public string Description { get; set; }

        public DateTime StartTime { get; set; }

        [JsonIgnore]
        public Action RetryAction { get; set; }
    }

    public class BackgroundOperation<T> : BackgroundOperation
    {
        [JsonIgnore]
        public Task<T> Task { get; set; }

        public BackgroundOperation()
        {
            OperationId = Guid.NewGuid();
            StartTime = DateTime.UtcNow;
        }
    }


    public enum OperationType
    {
        SubscriptionLoad,
        ResourceGroupDelete,
        ResourceGroupDeleteThenCreate,
        ResourceGroupCreate,
        ResourceGroupPutInDesiredState,
        LogUsageStatistics
    }
}