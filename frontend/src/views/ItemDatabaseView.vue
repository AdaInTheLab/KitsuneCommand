<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { getGameItems, getGameItemGroups, getGameItemIconUrl } from '@/api/gameItems'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import type { GameItemInfo } from '@/types'

const { t } = useI18n()
const toast = useToast()

const loading = ref(true)
const items = ref<GameItemInfo[]>([])
const searchFilter = ref('')
const groupFilter = ref<string | null>(null)
const groups = ref<{ label: string; value: string | null }[]>([])
const pageIndex = ref(0)
const pageSize = ref(50)
const totalRecords = ref(0)
let searchTimeout: ReturnType<typeof setTimeout> | null = null

async function fetchItems() {
  loading.value = true
  try {
    const result = await getGameItems({
      search: searchFilter.value || undefined,
      group: groupFilter.value || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })
    items.value = result.items
    totalRecords.value = result.total
  } catch {
    toast.add({
      severity: 'error',
      summary: t('common.error'),
      detail: t('itemDatabase.failedToLoad'),
      life: 3000,
    })
  } finally {
    loading.value = false
  }
}

async function fetchGroups() {
  try {
    const data = await getGameItemGroups()
    groups.value = [
      { label: t('itemDatabase.allGroups'), value: null },
      ...data.map((g) => ({ label: g, value: g })),
    ]
  } catch {
    // Groups are non-critical
  }
}

function onSearch() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    pageIndex.value = 0
    fetchItems()
  }, 300)
}

function onGroupChange() {
  pageIndex.value = 0
  fetchItems()
}

function onPage(event: { first: number; rows: number }) {
  pageIndex.value = Math.floor(event.first / event.rows)
  pageSize.value = event.rows
  fetchItems()
}

watch(searchFilter, onSearch)

onMounted(() => {
  fetchItems()
  fetchGroups()
})
</script>

<template>
  <div class="item-database-view">
    <div class="page-header">
      <h1 class="page-title">{{ t('itemDatabase.title') }}</h1>
      <span class="page-subtitle">{{ t('itemDatabase.subtitle') }}</span>
    </div>

    <div class="toolbar">
      <span class="p-input-icon-left search-wrapper">
        <i class="pi pi-search" />
        <InputText
          v-model="searchFilter"
          :placeholder="t('itemDatabase.searchPlaceholder')"
          class="search-input"
        />
      </span>
      <Select
        v-model="groupFilter"
        :options="groups"
        optionLabel="label"
        optionValue="value"
        :placeholder="t('itemDatabase.allGroups')"
        class="group-filter"
        @change="onGroupChange"
      />
      <Button icon="pi pi-refresh" text severity="secondary" @click="fetchItems" :loading="loading" />
      <span class="total-count" v-if="!loading">
        {{ t('itemDatabase.totalItems') }}: {{ totalRecords.toLocaleString() }}
      </span>
    </div>

    <DataTable
      :value="items"
      :loading="loading"
      stripedRows
      :paginator="true"
      :rows="pageSize"
      :totalRecords="totalRecords"
      :lazy="true"
      :rowsPerPageOptions="[25, 50, 100]"
      @page="onPage"
      class="items-table"
    >
      <Column :header="t('itemDatabase.columnIcon')" style="width: 60px">
        <template #body="{ data }">
          <img
            v-if="data.iconName"
            :src="getGameItemIconUrl(data.iconName, 40)"
            :alt="data.iconName"
            class="item-icon"
            loading="lazy"
            @error="($event.target as HTMLImageElement).style.display = 'none'"
          />
        </template>
      </Column>

      <Column field="displayName" :header="t('itemDatabase.columnDisplayName')" sortable>
        <template #body="{ data }">
          <div class="item-name-cell">
            <span class="item-display-name">{{ data.displayName }}</span>
            <span class="item-internal-name">{{ data.itemName }}</span>
          </div>
        </template>
      </Column>

      <Column field="hasQuality" :header="t('itemDatabase.columnQuality')" style="width: 120px">
        <template #body="{ data }">
          <Tag v-if="data.hasQuality" :value="t('common.yes')" severity="info" />
          <Tag v-else :value="t('common.no')" severity="secondary" />
        </template>
      </Column>

      <Column field="maxStack" :header="t('itemDatabase.columnMaxStack')" style="width: 120px">
        <template #body="{ data }">
          <span>{{ data.maxStack }}</span>
        </template>
      </Column>

      <Column field="groups" :header="t('itemDatabase.columnGroups')">
        <template #body="{ data }">
          <div class="groups-list">
            <Tag v-for="g in data.groups" :key="g" :value="g" severity="secondary" class="group-tag" />
          </div>
        </template>
      </Column>

      <template #empty>
        <div class="empty-state">
          <i class="pi pi-database" style="font-size: 2rem; color: var(--kc-text-secondary)" />
          <p>{{ t('itemDatabase.noItemsFound') }}</p>
        </div>
      </template>
    </DataTable>
  </div>
</template>

<style scoped>
.item-database-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.page-header {
  display: flex;
  align-items: baseline;
  gap: 1rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
}

.page-subtitle {
  font-size: 0.9rem;
  color: var(--kc-text-secondary);
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.search-wrapper {
  flex: 1;
  max-width: 350px;
}

.search-input {
  width: 100%;
}

.group-filter {
  min-width: 180px;
}

.total-count {
  margin-left: auto;
  font-size: 0.85rem;
  color: var(--kc-text-secondary);
  white-space: nowrap;
}

.item-icon {
  width: 40px;
  height: 40px;
  object-fit: contain;
  display: block;
}

.item-name-cell {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.item-display-name {
  font-weight: 500;
}

.item-internal-name {
  font-size: 0.75rem;
  color: var(--kc-text-secondary);
}

.groups-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}

.group-tag {
  font-size: 0.75rem;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem;
  color: var(--kc-text-secondary);
}

@media (max-width: 768px) {
  .toolbar {
    flex-wrap: wrap;
    width: 100%;
  }
  .search-wrapper {
    max-width: none;
    flex: 1 1 100%;
  }
  .group-filter {
    flex: 1 1 100%;
  }
  .page-header {
    flex-direction: column;
    gap: 0.25rem;
  }
}
</style>
