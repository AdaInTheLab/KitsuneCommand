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
import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
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

const isDirty = computed(() => {
  if (activeTab.value === 'raw') {
    return rawXml.value !== rawXmlOriginal.value
  }
  return JSON.stringify(properties.value) !== JSON.stringify(originalProperties.value)
})

const groupLabels: Record<string, string> = {
  core: 'config.group.core',
  world: 'config.group.world',
  blockDamage: 'config.group.blockDamage',
  gameplay: 'config.group.gameplay',
  zombies: 'config.group.zombies',
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
      // Reload form properties after raw save
      loadConfig()
    } else {
      // Only send changed properties
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
  return properties.value[key] ?? ''
}

function setFieldValue(key: string, value: string) {
  properties.value[key] = value
}

function getBoolValue(key: string): boolean {
  const val = getFieldValue(key).toLowerCase()
  return val === 'true' || val === '1'
}

function setBoolValue(key: string, value: boolean) {
  setFieldValue(key, value ? 'true' : 'false')
}

function getSelectOptions(field: { key: string; options?: string[] }) {
  // Special case: GameWorld includes detected worlds
  if (field.key === 'GameWorld') {
    const opts = [...(field.options || []), ...worlds.value]
    return [...new Set(opts)].map(v => ({ label: v, value: v }))
  }
  return (field.options || []).map(v => ({ label: v, value: v }))
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
      <Accordion :multiple="true" :value="['0', '1']">
        <AccordionPanel v-for="(group, idx) in groups" :key="group.key" :value="String(idx)">
          <AccordionHeader>{{ t(groupLabels[group.key] || group.key) }}</AccordionHeader>
          <AccordionContent>
            <div class="field-grid">
              <div v-for="field in group.fields" :key="field.key" class="field-item">
                <label class="field-label">{{ field.key }}</label>

                <!-- Text field -->
                <InputText
                  v-if="field.type === 'text'"
                  :modelValue="getFieldValue(field.key)"
                  @update:modelValue="setFieldValue(field.key, String($event ?? ''))"
                  class="field-input"
                />

                <!-- Number field -->
                <InputNumber
                  v-else-if="field.type === 'number'"
                  :modelValue="Number(getFieldValue(field.key)) || 0"
                  @update:modelValue="setFieldValue(field.key, String($event ?? 0))"
                  :min="field.min ?? undefined"
                  :max="field.max ?? undefined"
                  class="field-input"
                />

                <!-- Boolean field -->
                <div v-else-if="field.type === 'bool'" class="bool-field">
                  <ToggleSwitch
                    :modelValue="getBoolValue(field.key)"
                    @update:modelValue="setBoolValue(field.key, $event)"
                  />
                  <span class="bool-label">{{ getBoolValue(field.key) ? t('common.enabled') : t('common.disabled') }}</span>
                </div>

                <!-- Select field -->
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
          </AccordionContent>
        </AccordionPanel>
      </Accordion>
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

.field-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1rem;
  padding: 0.5rem 0;
}

.field-item {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--kc-text-secondary);
  font-family: monospace;
}

.field-input {
  width: 100%;
}

.bool-field {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding-top: 0.25rem;
}

.bool-label {
  font-size: 0.85rem;
  color: var(--kc-text-secondary);
}

.raw-view {
  flex: 1;
}

.raw-textarea {
  width: 100%;
  font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
  font-size: 0.85rem;
  background: #0d1117;
  color: #e6edf3;
  border: 1px solid var(--kc-border);
  border-radius: 8px;
  padding: 1rem;
  resize: vertical;
  min-height: 400px;
}

@media (max-width: 768px) {
  .page-header { flex-direction: column; }
  .header-actions { flex-wrap: wrap; }
  .field-grid { grid-template-columns: 1fr; }
}
</style>
