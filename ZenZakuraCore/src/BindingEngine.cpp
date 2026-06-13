#include "BindingEngine.h"

BindingEngine::BindingEngine() {
    InitializeCriticalSection(&m_cs);
}

BindingEngine::~BindingEngine() {
    DeleteCriticalSection(&m_cs);
}

void BindingEngine::Register(uint32_t vk, const ZenKeyEvent* events, int count, ZenPlayMode mode) {
    EnterCriticalSection(&m_cs);
    Binding& b = m_bindings[vk];
    b.events.assign(events, events + count);
    b.mode = mode;
    b.repeatDelayMs = 0;
    LeaveCriticalSection(&m_cs);
}

void BindingEngine::Unregister(uint32_t vk) {
    EnterCriticalSection(&m_cs);
    m_bindings.erase(vk);
    LeaveCriticalSection(&m_cs);
}

void BindingEngine::Clear() {
    EnterCriticalSection(&m_cs);
    m_bindings.clear();
    LeaveCriticalSection(&m_cs);
}

Binding* BindingEngine::Find(uint32_t vk) {
    EnterCriticalSection(&m_cs);
    auto it = m_bindings.find(vk);
    if (it != m_bindings.end()) {
        LeaveCriticalSection(&m_cs);
        return &it->second;
    }
    LeaveCriticalSection(&m_cs);
    return nullptr;
}

bool BindingEngine::IsBound(uint32_t vk) {
    return Find(vk) != nullptr;
}
