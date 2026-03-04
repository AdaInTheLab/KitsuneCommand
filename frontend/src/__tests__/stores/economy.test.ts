import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useEconomyStore } from '@/stores/economy'

describe('Economy Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('should initialize with empty state', () => {
    const economy = useEconomyStore()
    expect(economy.pointsList).toEqual([])
    expect(economy.pointsTotal).toBe(0)
  })

  it('setPointsList should update list and total', () => {
    const economy = useEconomyStore()

    const items = [
      { id: 'p1', playerName: 'Alice', points: 100, createdAt: '', lastSignInAt: null },
      { id: 'p2', playerName: 'Bob', points: 50, createdAt: '', lastSignInAt: null },
    ]

    economy.setPointsList(items as any, 2)

    expect(economy.pointsList).toHaveLength(2)
    expect(economy.pointsTotal).toBe(2)
    expect(economy.pointsList[0].playerName).toBe('Alice')
  })

  it('handlePointsUpdate should update existing player in list', () => {
    const economy = useEconomyStore()

    economy.setPointsList(
      [
        { id: 'p1', playerName: 'Alice', points: 100, createdAt: '', lastSignInAt: null },
        { id: 'p2', playerName: 'Bob', points: 50, createdAt: '', lastSignInAt: null },
      ] as any,
      2,
    )

    economy.handlePointsUpdate({
      playerId: 'p1',
      playerName: 'Alice',
      points: 150,
      change: 50,
      reason: 'bonus',
    })

    expect(economy.pointsList[0].points).toBe(150)
    expect(economy.pointsList[1].points).toBe(50) // Unchanged
  })

  it('handlePointsUpdate should update player name if changed', () => {
    const economy = useEconomyStore()

    economy.setPointsList(
      [{ id: 'p1', playerName: 'OldName', points: 100, createdAt: '', lastSignInAt: null }] as any,
      1,
    )

    economy.handlePointsUpdate({
      playerId: 'p1',
      playerName: 'NewName',
      points: 100,
      change: 0,
      reason: 'rename',
    })

    expect(economy.pointsList[0].playerName).toBe('NewName')
  })

  it('handlePointsUpdate should ignore players not in current list', () => {
    const economy = useEconomyStore()

    economy.setPointsList(
      [{ id: 'p1', playerName: 'Alice', points: 100, createdAt: '', lastSignInAt: null }] as any,
      1,
    )

    // This player isn't in the list - should not throw or modify anything
    economy.handlePointsUpdate({
      playerId: 'p999',
      playerName: 'Unknown',
      points: 50,
      change: 50,
      reason: 'gift',
    })

    expect(economy.pointsList).toHaveLength(1)
    expect(economy.pointsList[0].id).toBe('p1')
  })

  it('clearPoints should reset state', () => {
    const economy = useEconomyStore()

    economy.setPointsList(
      [{ id: 'p1', playerName: 'Alice', points: 100, createdAt: '', lastSignInAt: null }] as any,
      1,
    )

    economy.clearPoints()

    expect(economy.pointsList).toEqual([])
    expect(economy.pointsTotal).toBe(0)
  })
})
