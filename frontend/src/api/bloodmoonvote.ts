import apiClient from './client'
import type { BloodMoonVoteStatus, BloodMoonVoteSettings } from '@/types'

export async function getVoteStatus(): Promise<BloodMoonVoteStatus> {
  const response = await apiClient.get('/api/bloodmoonvote/status')
  return response.data.data
}

export async function getVoteSettings(): Promise<BloodMoonVoteSettings> {
  const response = await apiClient.get('/api/bloodmoonvote/settings')
  return response.data.data
}

export async function updateVoteSettings(settings: BloodMoonVoteSettings): Promise<void> {
  await apiClient.put('/api/bloodmoonvote/settings', settings)
}

export async function forceSkipBloodMoon(): Promise<void> {
  await apiClient.post('/api/bloodmoonvote/force-skip')
}
