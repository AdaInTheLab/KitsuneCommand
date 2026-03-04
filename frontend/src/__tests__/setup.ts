import { vi } from 'vitest'

// Mock vue-i18n — return the key as the translation
vi.mock('vue-i18n', () => ({
  useI18n: () => ({
    t: (key: string) => key,
    locale: { value: 'en' },
  }),
  createI18n: () => ({
    global: { t: (key: string) => key },
  }),
}))

// Mock PrimeVue useToast
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: vi.fn(),
  }),
}))
