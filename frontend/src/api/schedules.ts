import apiClient from './client'
import type { TaskScheduleDetail } from '@/types'
import type { PaginatedResponse } from './store'

// ─── Task Schedule CRUD ─────────────────────────────

export async function getSchedules(params: {
  pageIndex?: number
  pageSize?: number
  search?: string
} = {}): Promise<PaginatedResponse<TaskScheduleDetail>> {
  const response = await apiClient.get('/api/schedules', { params })
  return response.data.data
}

export async function getScheduleDetail(id: number): Promise<TaskScheduleDetail> {
  const response = await apiClient.get(`/api/schedules/${id}`)
  return response.data.data
}

export async function createSchedule(data: {
  name: string
  description?: string
  intervalMinutes: number
  commandIds?: number[]
}): Promise<{ id: number }> {
  const response = await apiClient.post('/api/schedules', data)
  return response.data.data
}

export async function updateSchedule(id: number, data: {
  name: string
  description?: string
  intervalMinutes: number
  isEnabled: number
  commandIds?: number[]
}): Promise<{ id: number }> {
  const response = await apiClient.put(`/api/schedules/${id}`, data)
  return response.data.data
}

export async function deleteSchedule(id: number): Promise<void> {
  await apiClient.delete(`/api/schedules/${id}`)
}

// ─── Special Actions ────────────────────────────────

export async function runScheduleNow(id: number): Promise<{
  message: string
  results: string[]
}> {
  const response = await apiClient.post(`/api/schedules/${id}/run`)
  return response.data.data
}

export async function toggleSchedule(id: number): Promise<{
  id: number
  isEnabled: number
  message: string
}> {
  const response = await apiClient.post(`/api/schedules/${id}/toggle`)
  return response.data.data
}
