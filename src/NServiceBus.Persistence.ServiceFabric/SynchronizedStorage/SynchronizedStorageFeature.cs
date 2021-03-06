﻿namespace NServiceBus.Persistence.ServiceFabric
{
    using Features;

    class SynchronizedStorageFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var stateManager = context.Settings.StateManager();
            var container = context.Container;
            container.ConfigureComponent(b => new SynchronizedStorage(stateManager), DependencyLifecycle.SingleInstance);
            container.ConfigureComponent(b => new SynchronizedStorageAdapter(), DependencyLifecycle.SingleInstance);
        }
    }
}