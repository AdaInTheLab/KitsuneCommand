import apiClient from './client'
import type { ChatCommandSettings, PointsSettings, TeleportSettings, StoreSettings } from '@/types'

export async function getChatCommandSettings(): Promise<ChatCommandSettings> {
  const response = await apiClient.get('/api/settings/chat-commands')
  return response.data.data
}

export async function updateChatCommandSettings(settings: ChatCommandSettings): Promise<void> {
  await apiClient.put('/api/settings/chat-commands', settings)
}

export async function getPointsSettings(): Promise<PointsSettings> {
  const response = await apiClient.get('/api/settings/points')
  return response.data.data
}

export async function updatePointsSettings(settings: PointsSettings): Promise<void> {
  await apiClient.put('/api/settings/points', settings)
}

export async function getTeleportSettings(): Promise<TeleportSettings> {
  const response = await apiClient.get('/api/settings/teleport')
  return response.data.data
}

export async function updateTeleportSettings(settings: TeleportSettings): Promise<void> {
  await apiClient.put('/api/settings/teleport', settings)
}

export async function getStoreSettings(): Promise<StoreSettings> {
  const response = await apiClient.get('/api/settings/store')
  return response.data.data
}

export async function updateStoreSettings(settings: StoreSettings): Promise<void> {
  await apiClient.put('/api/settings/store', settings)
}
