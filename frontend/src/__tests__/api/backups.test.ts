import { describe, it, expect, vi, beforeEach } from 'vitest'

// Create mock axios instance
const mockGet = vi.fn()
const mockPost = vi.fn()
const mockPut = vi.fn()
const mockDelete = vi.fn()

// Mock the client module before importing the module under test
vi.mock('@/api/client', () => ({
  default: {
    get: (...args: any[]) => mockGet(...args),
    post: (...args: any[]) => mockPost(...args),
    put: (...args: any[]) => mockPut(...args),
    delete: (...args: any[]) => mockDelete(...args),
  },
}))

// Import AFTER mocking
import {
  getBackups,
  createBackup,
  restoreBackup,
  deleteBackup,
  getBackupSettings,
  saveBackupSettings,
} from '@/api/backups'

describe('Backups API', () => {
  beforeEach(() => {
    mockGet.mockReset()
    mockPost.mockReset()
    mockPut.mockReset()
    mockDelete.mockReset()
  })

  describe('getBackups', () => {
    it('should call GET /api/backups and unwrap data', async () => {
      const backups = [
        { id: 1, filename: 'backup_1.zip', worldName: 'Navezgane', sizeBytes: 1024 },
      ]
      mockGet.mockResolvedValue({ data: { data: backups } })

      const result = await getBackups()

      expect(mockGet).toHaveBeenCalledWith('/api/backups')
      expect(result).toEqual(backups)
    })

    it('should return undefined when API returns error response', async () => {
      mockGet.mockResolvedValue({ data: { code: 500, message: 'error' } })

      const result = await getBackups()

      // res.data.data is undefined when error response has no 'data' field
      expect(result).toBeUndefined()
    })
  })

  describe('createBackup', () => {
    it('should call POST /api/backups with notes', async () => {
      const newBackup = { id: 2, filename: 'backup_2.zip' }
      mockPost.mockResolvedValue({ data: { data: newBackup } })

      const result = await createBackup('test notes')

      expect(mockPost).toHaveBeenCalledWith('/api/backups', { notes: 'test notes' })
      expect(result).toEqual(newBackup)
    })

    it('should call POST /api/backups without notes', async () => {
      mockPost.mockResolvedValue({ data: { data: {} } })

      await createBackup()

      expect(mockPost).toHaveBeenCalledWith('/api/backups', { notes: undefined })
    })
  })

  describe('restoreBackup', () => {
    it('should call POST /api/backups/{id}/restore', async () => {
      mockPost.mockResolvedValue({ data: { message: 'Restored' } })

      await restoreBackup(5)

      expect(mockPost).toHaveBeenCalledWith('/api/backups/5/restore')
    })
  })

  describe('deleteBackup', () => {
    it('should call DELETE /api/backups/{id}', async () => {
      mockDelete.mockResolvedValue({ data: { message: 'Deleted' } })

      await deleteBackup(3)

      expect(mockDelete).toHaveBeenCalledWith('/api/backups/3')
    })
  })

  describe('getBackupSettings', () => {
    it('should call GET /api/backups/settings and unwrap data', async () => {
      const settings = { enabled: true, intervalMinutes: 60, maxBackups: 10, backupPath: 'Backups' }
      mockGet.mockResolvedValue({ data: { data: settings } })

      const result = await getBackupSettings()

      expect(mockGet).toHaveBeenCalledWith('/api/backups/settings')
      expect(result).toEqual(settings)
    })
  })

  describe('saveBackupSettings', () => {
    it('should call PUT /api/backups/settings with settings object', async () => {
      const settings = { enabled: true, intervalMinutes: 30, maxBackups: 5, backupPath: 'MyBackups' }
      mockPut.mockResolvedValue({ data: { message: 'Updated' } })

      await saveBackupSettings(settings)

      expect(mockPut).toHaveBeenCalledWith('/api/backups/settings', settings)
    })
  })
})
