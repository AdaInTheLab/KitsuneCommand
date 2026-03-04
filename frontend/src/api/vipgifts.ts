import apiClient from './client'
import type { VipGift, VipGiftDetail } from '@/types'
import type { PaginatedResponse } from './store'

// ─── VIP Gift CRUD ──────────────────────────────────

export async function getVipGifts(params: {
  pageIndex?: number
  pageSize?: number
  search?: string
} = {}): Promise<PaginatedResponse<VipGift>> {
  const response = await apiClient.get('/api/vipgifts', { params })
  return response.data.data
}

export async function getVipGiftDetail(id: number): Promise<VipGiftDetail> {
  const response = await apiClient.get(`/api/vipgifts/${id}`)
  return response.data.data
}

export async function createVipGift(data: {
  playerId: string
  playerName?: string
  name: string
  description?: string
  claimPeriod?: string | null
  itemIds?: number[]
  commandIds?: number[]
}): Promise<{ id: number }> {
  const response = await apiClient.post('/api/vipgifts', data)
  return response.data.data
}

export async function updateVipGift(id: number, data: {
  name: string
  description?: string
  claimPeriod?: string | null
  playerName?: string
  itemIds?: number[]
  commandIds?: number[]
}): Promise<{ id: number }> {
  const response = await apiClient.put(`/api/vipgifts/${id}`, data)
  return response.data.data
}

export async function deleteVipGift(id: number): Promise<void> {
  await apiClient.delete(`/api/vipgifts/${id}`)
}

// ─── Claiming ───────────────────────────────────────

export async function claimVipGift(id: number): Promise<{
  message: string
  deliveryLog: string[]
}> {
  const response = await apiClient.post(`/api/vipgifts/${id}/claim`)
  return response.data.data
}
