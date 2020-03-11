using System;
using System.Collections.Generic;
using Gamebench.Sdk;

public interface IGamebenchShared
{
    //this interface is using internally, not expose to customer
    void InjectFPS(uint fps);
    void MarkStartCurrentScene();
    void MarkStopCurrentScene();
    void CleanupSessionDataIfTimeTooShort();
    void GPUStart(); //only for android
    void GPUStop();
    void MemoryStart();
    void MemoryStop();
    void BindWithAutoMode(GamebenchSDKAuto binder);
    void UnBindWithAutoMode();
    void InjectMemoryMetric();
    void UpdateMetricRuntime(float delta);
    object GetfrequencyRunTime();
}
