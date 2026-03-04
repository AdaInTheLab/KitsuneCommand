import apiClient from './client'
import type { GameItemInfo } from '@/types'
import type { PaginatedResponse } from './store'

export async function getGameItems(
  params: {
    search?: string
    group?: string
    pageIndex?: number
    pageSize?: number
  } = {},
): Promise<PaginatedResponse<GameItemInfo>> {
  const response = await apiClient.get('/api/game-items', { params })
  return response.data.data
}

export async function getGameItemGroups(): Promise<string[]> {
  const response = await apiClient.get('/api/game-items/groups')
  return response.data.data
}

export async function searchGameItems(query: string, limit: number = 20): Promise<GameItemInfo[]> {
  const response = await apiClient.get('/api/game-items/search', { params: { query, limit } })
  return response.data.data
}

/**
 * Returns the URL for a game item icon image.
 * Icons are served from the backend as static PNGs with 7-day cache.
 */
export function getGameItemIconUrl(iconName: string, size?: number): string {
  if (!iconName) return ''
  const params = size ? `?size=${size}` : ''
  return `/api/game-items/icon/${encodeURIComponent(iconName)}${params}`
}
