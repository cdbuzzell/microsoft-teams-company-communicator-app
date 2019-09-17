﻿// <copyright file="CompanyCommunicatorPretreatFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for preparing to send a notification.
    /// </summary>
    public class CompanyCommunicatorPretreatFunction
    {
        private const string QueueName = "company-communicator-pretreat";
        private const string ConnectionName = "ServiceBusConnection";
        private readonly NotificationDataRepository notificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyCommunicatorPretreatFunction"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification data repository service.</param>
        public CompanyCommunicatorPretreatFunction(
            NotificationDataRepository notificationDataRepository)
        {
            this.notificationDataRepository = notificationDataRepository;
        }

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for kicking off the notificaiton pretreatment.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="starter">Durable orchestration client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorPretreatFunction")]
        public async Task Run(
            [ServiceBusTrigger(
                CompanyCommunicatorPretreatFunction.QueueName,
                Connection = CompanyCommunicatorPretreatFunction.ConnectionName)]
            string myQueueItem,
            [OrchestrationClient]
            DurableOrchestrationClient starter)
        {
            var partitionKey = PartitionKeyNames.NotificationDataTable.DraftNotificationsPartition;
            var notificationDataEntityId = JsonConvert.DeserializeObject<string>(myQueueItem);
            var notificationDataEntity =
                await this.notificationDataRepository.GetAsync(partitionKey, notificationDataEntityId);
            if (notificationDataEntity != null)
            {
                string instanceId = await starter.StartNewAsync(
                    nameof(DeliveryPretreatmentOrchestration.StartOrchestrationAsync),
                    notificationDataEntity);
            }
        }
    }
}