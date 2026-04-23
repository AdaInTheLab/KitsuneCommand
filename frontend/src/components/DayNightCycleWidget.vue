<script setup lang="ts">
import { computed } from 'vue'
import Slider from 'primevue/slider'
import InputNumber from 'primevue/inputnumber'
import { resolveDayNightConfig, reverseDayNightConfig } from '@/lib/dayNight'

/**
 * Visual editor for the DayNightLength + DayLightLength pair.
 *
 * Rather than asking operators to think in "total real-time minutes per cycle" and
 * "in-game hours of daylight", this widget presents the same information as
 * "day minutes" and "night minutes" — values you can feel. The underlying raw
 * config values update in lockstep.
 */

const props = defineProps<{
  dayNightLength: number  // real-time minutes per 24h cycle, 10-240
  dayLightLength: number  // in-game hours of daylight, 1-23
}>()

const emit = defineEmits<{
  (e: 'update', config: { DayNightLength: number; DayLightLength: number }): void
}>()

const friendly = computed(() =>
  reverseDayNightConfig(props.dayNightLength, props.dayLightLength),
)

const total = computed(() => friendly.value.dayMinutes + friendly.value.nightMinutes)
const dayPct = computed(() => (friendly.value.dayMinutes / total.value) * 100)

function pushUpdate(dayMinutes: number, nightMinutes: number) {
  try {
    const cfg = resolveDayNightConfig({ dayMinutes, nightMinutes })
    emit('update', cfg)
  } catch {
    // values out of range — ignore, keep current
  }
}

function onDayChange(value: number | null) {
  if (value == null) return
  pushUpdate(Math.max(1, value), friendly.value.nightMinutes)
}

function onNightChange(value: number | null) {
  if (value == null) return
  pushUpdate(friendly.value.dayMinutes, Math.max(1, value))
}

function onSliderChange(value: number | number[]) {
  const pct = Array.isArray(value) ? value[0] : value
  const tot = total.value
  const dayMinutes = Math.max(1, Math.round((pct / 100) * tot))
  const nightMinutes = Math.max(1, tot - dayMinutes)
  pushUpdate(dayMinutes, nightMinutes)
}
</script>

<template>
  <div class="daynight-widget">
    <div class="daynight-header">
      <div class="daynight-title">Day / Night Cycle</div>
      <div class="daynight-summary">
        {{ friendly.dayMinutes }} min day · {{ friendly.nightMinutes }} min night
      </div>
    </div>

    <!-- Visual bar: day (warm) vs night (cool) -->
    <div class="daynight-bar" role="img" aria-label="Day versus night split">
      <div class="daynight-day" :style="{ flexBasis: dayPct + '%' }">
        <span v-if="dayPct >= 15">☀ {{ friendly.dayMinutes }}m</span>
      </div>
      <div class="daynight-night" :style="{ flexBasis: (100 - dayPct) + '%' }">
        <span v-if="100 - dayPct >= 15">☾ {{ friendly.nightMinutes }}m</span>
      </div>
    </div>

    <!-- Slider for the split -->
    <div class="daynight-slider">
      <Slider
        :modelValue="dayPct"
        :min="5"
        :max="95"
        :step="1"
        @update:modelValue="onSliderChange"
      />
    </div>

    <!-- Fine controls — slider is the primary, these are for exact entry -->
    <div class="daynight-fields">
      <div class="daynight-field">
        <label>Day</label>
        <InputNumber
          :modelValue="friendly.dayMinutes"
          :min="1"
          :max="239"
          showButtons
          buttonLayout="horizontal"
          :inputStyle="{ width: '3.5rem', textAlign: 'center' }"
          @update:modelValue="onDayChange"
          class="daynight-input"
        />
      </div>
      <div class="daynight-field">
        <label>Night</label>
        <InputNumber
          :modelValue="friendly.nightMinutes"
          :min="1"
          :max="239"
          showButtons
          buttonLayout="horizontal"
          :inputStyle="{ width: '3.5rem', textAlign: 'center' }"
          @update:modelValue="onNightChange"
          class="daynight-input"
        />
      </div>
    </div>

    <small class="daynight-raw">
      Raw values: <code>DayNightLength={{ dayNightLength }}</code> ·
      <code>DayLightLength={{ dayLightLength }}h</code>
    </small>
  </div>
</template>

<style scoped>
.daynight-widget {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--p-surface-800, #1f2937);
  border: 1px solid var(--p-surface-700, #374151);
  border-radius: 0.5rem;
}

.daynight-header {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 1rem;
}

.daynight-title {
  font-weight: 600;
  font-size: 0.95rem;
}

.daynight-summary {
  color: var(--p-text-muted-color, #9ca3af);
  font-size: 0.85rem;
  font-variant-numeric: tabular-nums;
}

.daynight-bar {
  display: flex;
  height: 2.25rem;
  border-radius: 0.375rem;
  overflow: hidden;
  font-size: 0.8rem;
  font-weight: 500;
  user-select: none;
}

.daynight-day {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(180deg, #fbbf24 0%, #f59e0b 100%);
  color: #451a03;
  min-width: 2rem;
  transition: flex-basis 0.2s ease;
}

.daynight-night {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(180deg, #312e81 0%, #1e1b4b 100%);
  color: #e0e7ff;
  min-width: 2rem;
  transition: flex-basis 0.2s ease;
}

.daynight-slider {
  padding: 0 0.25rem;
}

.daynight-fields {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  justify-content: flex-start;
  flex-wrap: wrap;
}

.daynight-field {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
}

/* Horizontal +/- buttons on either side of the number — keep them narrow */
.daynight-input :deep(.p-inputnumber-button) {
  min-width: 1.75rem;
  width: 1.75rem;
  padding: 0;
}

/* Stack vertically when the card is narrow (1-col grid on smaller screens) */
@container (max-width: 320px) {
  .daynight-fields {
    grid-template-columns: 1fr;
  }
}

.daynight-widget {
  container-type: inline-size;
}

.daynight-field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.daynight-field label {
  font-size: 0.75rem;
  color: var(--p-text-muted-color, #9ca3af);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  min-width: 2.5rem;
}

.daynight-raw {
  color: var(--p-text-muted-color, #9ca3af);
  font-size: 0.7rem;
}

.daynight-raw code {
  background: var(--p-surface-900, #111827);
  padding: 1px 4px;
  border-radius: 3px;
  font-size: 0.7rem;
}
</style>
