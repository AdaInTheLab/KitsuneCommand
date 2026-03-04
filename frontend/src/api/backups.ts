import apiClient from './client'

export interface BackupRecord {
  id: number
  filename: string
  worldName: string
  sizeBytes: number
  createdAt: string
  backupType: string
  notes: string | null
}

export interface BackupSettings {
  enabled: boolean
  intervalMinutes: number
  maxBackups: number
  backupPath: string
}

export async function getBackups(): Promise<BackupRecord[]> {
  const res = await apiClient.get('/api/backups')
  return res.data.data
}

export async function createBackup(notes?: string): Promise<BackupRecord> {
  const res = await apiClient.post('/api/backups', { notes })
  return res.data.data
}

export async function restoreBackup(id: number): Promise<string> {
  const res = await apiClient.post(`/api/backups/${id}/restore`)
  return res.data.message
}

export async function deleteBackup(id: number): Promise<string> {
  const res = await apiClient.delete(`/api/backups/${id}`)
  return res.data.message
}

export async function getBackupSettings(): Promise<BackupSettings> {
  const res = await apiClient.get('/api/backups/settings')
  return res.data.data
}

export async function saveBackupSettings(settings: BackupSettings): Promise<string> {
  const res = await apiClient.put('/api/backups/settings', settings)
  return res.data.message
}
