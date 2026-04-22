<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useToast } from 'primevue/usetoast'
import { getConfig, getRawXml, saveConfig, saveRawXml, getWorlds, type ConfigFieldGroup } from '@/api/config'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import ToggleSwitch from 'primevue/toggleswitch'
import Textarea from 'primevue/textarea'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'

const { t } = useI18n()
const toast = useToast()

const loading = ref(true)
const saving = ref(false)
const activeTab = ref<'form' | 'raw'>('form')
const properties = ref<Record<string, string>>({})
const originalProperties = ref<Record<string, string>>({})
const groups = ref<ConfigFieldGroup[]>([])
const configPath = ref('')
const rawXml = ref('')
const rawXmlOriginal = ref('')
const worlds = ref<string[]>([])
const confirmDialogVisible = ref(false)
const passwordVisible = ref<Record<string, boolean>>({})

const isDirty = computed(() => {
  if (activeTab.value === 'raw') {
    return rawXml.value !== rawXmlOriginal.value
  }
  return JSON.stringify(properties.value) !== JSON.stringify(originalProperties.value)
})

const coreGroup = computed(() => groups.value.find(g => g.key === 'core'))
const otherGroups = computed(() => groups.value.filter(g => g.key !== 'core'))

const groupLabels: Record<string, string> = {
  core: 'config.group.core',
  world: 'config.group.world',
  blockDamage: 'config.group.blockDamage',
  gameplay: 'config.group.gameplay',
  zombies: 'config.group.zombies',
  bloodMoon: 'config.group.bloodMoon',
  lootAndDrops: 'config.group.lootAndDrops',
  landClaims: 'config.group.landClaims',
  networkAndSlots: 'config.group.networkAndSlots',
  admin: 'config.group.admin',
  advanced: 'config.group.advanced',
}

async function loadConfig() {
  loading.value = true
  try {
    const [configData, worldList] = await Promise.all([getConfig(), getWorlds()])
    properties.value = { ...configData.properties }
    originalProperties.value = { ...configData.properties }
    groups.value = configData.groups
    configPath.value = configData.configPath
    worlds.value = worldList
  } catch (err) {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('config.failedToLoad'), life: 4000 })
  } finally {
    loading.value = false
  }
}

async function loadRawXml() {
  try {
    rawXml.value = await getRawXml()
    rawXmlOriginal.value = rawXml.value
  } catch (err) {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('config.failedToLoad'), life: 4000 })
  }
}

function switchTab(tab: 'form' | 'raw') {
  activeTab.value = tab
  if (tab === 'raw' && !rawXml.value) {
    loadRawXml()
  }
}

function showSaveDialog() {
  confirmDialogVisible.value = true
}

async function handleSave() {
  confirmDialogVisible.value = false
  saving.value = true
  try {
    if (activeTab.value === 'raw') {
      await saveRawXml(rawXml.value)
      rawXmlOriginal.value = rawXml.value
      loadConfig()
    } else {
      const changed: Record<string, string> = {}
      for (const key of Object.keys(properties.value)) {
        if (properties.value[key] !== originalProperties.value[key]) {
          changed[key] = properties.value[key]
        }
      }
      if (Object.keys(changed).length === 0) {
        toast.add({ severity: 'info', summary: t('config.noChanges'), life: 2000 })
        return
      }
      await saveConfig(changed)
      originalProperties.value = { ...properties.value }
    }
    toast.add({ severity: 'success', summary: t('common.success'), detail: t('config.saved'), life: 4000 })
  } catch (err) {
    toast.add({ severity: 'error', summary: t('common.error'), detail: t('config.failedToSave'), life: 4000 })
  } finally {
    saving.value = false
  }
}

function getFieldValue(key: string): string {
  const lower = key.toLowerCase()
  const match = Object.keys(properties.value).find(k => k.toLowerCase() === lower)
  return match ? properties.value[match] : ''
}

function setFieldValue(key: string, value: string) {
  const lower = key.toLowerCase()
  const match = Object.keys(properties.value).find(k => k.toLowerCase() === lower)
  properties.value[match ?? key] = value
}

function getBoolValue(key: string): boolean {
  const val = getFieldValue(key).toLowerCase()
  return val === 'true' || val === '1'
}

function setBoolValue(key: string, value: boolean) {
  setFieldValue(key, value ? 'true' : 'false')
}

function getSelectOptions(field: { key: string; options?: string[]; labels?: string[] }) {
  if (field.key === 'GameWorld') {
    const opts = [...(field.options || []), ...worlds.value]
    return [...new Set(opts)].map(v => ({ label: v, value: v }))
  }
  return (field.options || []).map((v, i) => ({
    label: field.labels?.[i] ?? v,
    value: v
  }))
}

onMounted(loadConfig)
</script>

<template>
  <div class="config-editor-view">
    <div class="page-header">
      <div>
        <h1 class="page-title">{{ t('config.title') }}</h1>
        <p class="config-path" v-if="configPath">{{ configPath }}</p>
      </div>
      <div class="header-actions">
        <div class="tab-buttons">
          <Button
            :label="t('config.formView')"
            :severity="activeTab === 'form' ? 'info' : 'secondary'"
            size="small"
            @click="switchTab('form')"
          />
          <Button
            :label="t('config.rawView')"
            :severity="activeTab === 'raw' ? 'info' : 'secondary'"
            size="small"
            @click="switchTab('raw')"
          />
        </div>
        <Button
          :label="t('config.saveChanges')"
          icon="pi pi-save"
          severity="info"
          :disabled="!isDirty || saving"
          :loading="saving"
          @click="showSaveDialog"
        />
      </div>
    </div>

    <Message v-if="isDirty" severity="warn" :closable="false" class="dirty-banner">
      {{ t('config.unsavedChanges') }}
    </Message>

    <div v-if="loading" class="loading-state">
      <i class="pi pi-spin pi-spinner" style="font-size: 2rem" />
      <p>{{ t('common.loading') }}</p>
    </div>

    <!-- Form View -->
    <div v-else-if="activeTab === 'form'" class="form-view">
      <!-- Core Settings — prominent card with 2-col field grid -->
      <div v-if="coreGroup" class="core-card">
        <div class="core-header">
          <div class="core-accent" />
          <h3 class="group-title">{{ t(groupLabels[coreGroup.key] || coreGroup.key) }}</h3>
        </div>
        <div class="core-fields">
          <div v-for="field in coreGroup.fields" :key="field.key" class="field-item">
            <label class="field-label">{{ t('config.field.' + field.key, field.key) }}</label>
            <small v-if="field.description" class="field-description">{{ field.description }}</small>
            <InputText
              v-if="field.type === 'text'"
              :modelValue="getFieldValue(field.key)"
              @update:modelValue="setFieldValue(field.key, String($event ?? ''))"
              class="field-input"
            />
            <div v-else-if="field.type === 'password'" class="password-field">
              <InputText
                :modelValue="getFieldValue(field.key)"
                @update:modelValue="setFieldValue(field.key, String($event ?? ''))"
                :type="passwordVisible[field.key] ? 'text' : 'password'"
                class="field-input"
              />
              <button type="button" class="password-toggle" @click="passwordVisible[field.key] = !passwordVisible[field.key]">
                <i :class="passwordVisible[field.key] ? 'pi pi-eye-slash' : 'pi pi-eye'" />
              </button>
            </div>
            <Select
              v-else-if="field.type === 'select'"
              :modelValue="getFieldValue(field.key)"
              @update:modelValue="setFieldValue(field.key, $event)"
              :options="getSelectOptions(field)"
              optionLabel="label"
              optionValue="value"
              class="field-input"
            />
          </div>
        </div>
      </div>

      <!-- Other groups — 3-column card grid -->
      <div class="groups-grid">
        <div v-for="group in otherGroups" :key="group.key" class="group-card">
          <div class="group-card-header">
            {{ t(groupLabels[group.key] || group.key) }}
          </div>
          <div class="group-card-fields">
            <div v-for="field in group.fields" :key="field.key" class="field-item">
              <label class="field-label">{{ t('config.field.' + field.key, field.key) }}</label>
              <small v-if="field.description" class="field-description">{{ field.description }}</small>

              <InputText
                v-if="field.type === 'text'"
                :modelValue="getFieldValue(field.key)"
                @update:modelValue="setFieldValue(field.key, String($event ?? ''))"
                class="field-input"
              />
              <div v-else-if="field.type === 'password'" class="password-field">
                <InputText
                  :modelValue="getFieldValue(field.key)"
                  @update:modelValue="setFieldValue(field.key, String($event ?? ''))"
                  :type="passwordVisible[field.key] ? 'text' : 'password'"
                  class="field-input"
                />
                <button type="button" class="password-toggle" @click="passwordVisible[field.key] = !passwordVisible[field.key]">
                  <i :class="passwordVisible[field.key] ? 'pi pi-eye-slash' : 'pi pi-eye'" />
                </button>
              </div>
              <InputNumber
                v-else-if="field.type === 'number'"
                :modelValue="Number(getFieldValue(field.key)) || 0"
                @update:modelValue="setFieldValue(field.key, String($event ?? 0))"
                :min="field.min ?? undefined"
                :max="field.max ?? undefined"
                class="field-input"
              />
              <div v-else-if="field.type === 'bool'" class="bool-field">
                <ToggleSwitch
                  :modelValue="getBoolValue(field.key)"
                  @update:modelValue="setBoolValue(field.key, $event)"
                />
                <span class="bool-label">{{ getBoolValue(field.key) ? t('common.enabled') : t('common.disabled') }}</span>
              </div>
              <Select
                v-else-if="field.type === 'select'"
                :modelValue="getFieldValue(field.key)"
                @update:modelValue="setFieldValue(field.key, $event)"
                :options="getSelectOptions(field)"
                optionLabel="label"
                optionValue="value"
                class="field-input"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Raw XML View -->
    <div v-else class="raw-view">
      <Textarea
        v-model="rawXml"
        class="raw-textarea"
        :autoResize="false"
        rows="30"
      />
    </div>

    <!-- Save confirmation dialog -->
    <Dialog
      v-model:visible="confirmDialogVisible"
      :header="t('config.confirmSave')"
      modal
      :style="{ width: '420px' }"
    >
      <p>{{ t('config.restartRequired') }}</p>
      <template #footer>
        <Button :label="t('common.cancel')" severity="secondary" text @click="confirmDialogVisible = false" />
        <Button :label="t('config.saveChanges')" severity="info" @click="handleSave" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.config-editor-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0;
}

.config-path {
  font-size: 0.75rem;
  color: var(--kc-text-secondary);
  font-family: monospace;
  margin-top: 0.25rem;
  opacity: 0.7;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.tab-buttons {
  display: flex;
  gap: 0.25rem;
}

.dirty-banner {
  margin: 0;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  padding: 3rem;
  color: var(--kc-text-secondary);
}

.form-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* Core Settings — prominent card at top */
.core-card {
  background: linear-gradient(135deg, var(--kc-bg-secondary) 0%, var(--kc-bg-card) 100%);
  border: 1px solid var(--kc-border);
  border-radius: 12px;
  padding: 1.25rem;
}

.core-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.core-accent {
  width: 4px;
  height: 22px;
  border-radius: 4px;
  background: linear-gradient(to bottom, var(--kc-cyan), var(--kc-cyan-dark));
}

.group-title {
  font-size: 0.95rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--kc-text-primary);
  margin: 0;
}

.core-fields {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}

/* Group cards — 3-column grid */
.groups-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  align-items: start;
}

.group-card {
  background: rgba(26, 35, 50, 0.5);
  border: 1px solid var(--kc-border);
  border-radius: 12px;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.group-card-header {
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--kc-text-secondary);
  font-weight: 600;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid var(--kc-border);
}

.group-card-fields {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

/* Fields */
.field-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--kc-text-primary);
  font-family: monospace;
  letter-spacing: 0.02em;
}

.field-description {
  font-size: 0.65rem;
  color: var(--kc-text-secondary);
  opacity: 0.6;
  line-height: 1.3;
}

.field-input {
  width: 100%;
}

/* Form input dark theme */
.form-view :deep(.p-inputtext),
.form-view :deep(.p-inputnumber-input),
.form-view :deep(.p-select) {
  background: var(--kc-bg-primary);
  color: var(--kc-text-primary);
  border: 1px solid var(--kc-border);
  border-radius: 8px;
  font-size: 0.85rem;
  transition: border-color 0.15s;
}

.form-view :deep(.p-inputtext:focus),
.form-view :deep(.p-inputnumber-input:focus),
.form-view :deep(.p-select.p-focus) {
  border-color: var(--kc-cyan-dark);
  box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.15);
}

.form-view :deep(.p-select-label) {
  color: var(--kc-text-primary);
}

.form-view :deep(.p-select-dropdown) {
  color: var(--kc-text-secondary);
}

.password-field {
  position: relative;
  display: flex;
  align-items: center;
}

.password-toggle {
  position: absolute;
  right: 0.5rem;
  background: none;
  border: none;
  color: var(--kc-text-secondary);
  cursor: pointer;
  padding: 0.25rem;
  font-size: 0.9rem;
  opacity: 0.5;
  transition: opacity 0.15s;
}

.password-toggle:hover {
  opacity: 1;
  color: var(--kc-cyan);
}

.bool-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding-top: 0.15rem;
}

.bool-label {
  font-size: 0.8rem;
  color: var(--kc-text-secondary);
}

/* Raw XML */
.raw-view {
  flex: 1;
}

.raw-textarea {
  width: 100%;
  font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
  font-size: 0.85rem;
  background: var(--kc-bg-primary);
  color: #e6edf3;
  border: 1px solid var(--kc-border);
  border-radius: 10px;
  padding: 1rem;
  resize: vertical;
  min-height: 400px;
  line-height: 1.6;
}

.raw-textarea:focus {
  border-color: var(--kc-cyan-dark);
  outline: none;
  box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.15);
}

@media (max-width: 1200px) {
  .groups-grid { grid-template-columns: repeat(2, 1fr); }
}

@media (max-width: 768px) {
  .page-header { flex-direction: column; }
  .header-actions { flex-wrap: wrap; }
  .core-fields { grid-template-columns: 1fr; }
  .groups-grid { grid-template-columns: 1fr; }
}
</style>
