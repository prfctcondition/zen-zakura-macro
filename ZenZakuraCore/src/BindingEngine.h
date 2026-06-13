#pragma once
#include <windows.h>
#include <vector>
#include <unordered_map>
#include "../include/ZenZakuraCore.h"

struct Binding {
    std::vector<ZenKeyEvent> events;
    ZenPlayMode mode;
    int repeatDelayMs;
};

class BindingEngine {
public:
    BindingEngine();
    ~BindingEngine();

    void Register(uint32_t vk, const ZenKeyEvent* events, int count, ZenPlayMode mode);
    void Unregister(uint32_t vk);
    void Clear();
    Binding* Find(uint32_t vk);
    bool IsBound(uint32_t vk);

private:
    std::unordered_map<uint32_t, Binding> m_bindings;
    CRITICAL_SECTION m_cs;
};
