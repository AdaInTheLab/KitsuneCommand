import apiClient from './client'

export interface ModInfo {
  folderName: string
  displayName: string
  version: string | null
  author: string | null
  description: string | null
  website: string | null
  folderSize: number
  isEnabled: boolean
  isProtected: boolean
}

export async function getMods(): Promise<ModInfo[]> {
  const res = await apiClient.get('/api/mods')
  return res.data.data
}

export async function uploadMod(file: File): Promise<ModInfo> {
  const formData = new FormData()
  formData.append('file', file)
  const res = await apiClient.post('/api/mods/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 120000, // 2 min for large uploads
  })
  return res.data.data
}

export async function deleteMod(modName: string): Promise<string> {
  const res = await apiClient.delete(`/api/mods/${encodeURIComponent(modName)}`)
  return res.data.message
}

export async function toggleMod(modName: string): Promise<string> {
  const res = await apiClient.post(`/api/mods/${encodeURIComponent(modName)}/toggle`)
  return res.data.message
}
