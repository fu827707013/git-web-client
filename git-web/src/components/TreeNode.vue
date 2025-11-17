<template>
  <div class="tree-node">
    <!-- 文件夹节点 -->
    <div
      v-if="node.isFolder"
      class="tree-node-item folder-item"
      :style="{ paddingLeft: (level * 20) + 'px' }"
      @click="toggle"
    >
      <v-icon size="small" class="mr-1">
        {{ node.expanded ? 'mdi-chevron-down' : 'mdi-chevron-right' }}
      </v-icon>
      <v-icon size="small" :color="node.expanded ? 'amber-darken-2' : 'amber'" class="mr-2">
        {{ node.expanded ? 'mdi-folder-open' : 'mdi-folder' }}
      </v-icon>
      <span class="folder-name">{{ node.name }}</span>
      <v-chip size="x-small" class="ml-2" variant="text">
        {{ getFileCount(node) }}
      </v-chip>
    </div>

    <!-- 展开的子节点 -->
    <div v-if="node.isFolder && node.expanded" class="tree-children">
      <TreeNode
        v-for="child in node.children"
        :key="child.path || child.name"
        :node="child"
        :level="level + 1"
        :search-query="searchQuery"
        @toggle="$emit('toggle', $event)"
      />
    </div>

    <!-- 文件节点 -->
    <div
      v-if="!node.isFolder"
      class="tree-node-item file-item"
      :style="{ paddingLeft: ((level + 1) * 20) + 'px' }"
    >
      <span class="file-icon-placeholder"></span>
      <v-icon
        size="small"
        :color="getStatusColor(node.status)"
        class="mr-2"
      >
        {{ getStatusIcon(node.status) }}
      </v-icon>
      <span class="file-name">{{ node.name }}</span>

      <v-spacer></v-spacer>

      <v-chip
        :color="getStatusColor(node.status)"
        size="x-small"
        variant="flat"
        class="ml-2"
      >
        {{ node.status }}
      </v-chip>

      <div v-if="node.oldPath && node.oldPath !== node.path" class="file-renamed ml-2">
        <v-icon size="x-small">mdi-arrow-left</v-icon>
        <span class="text-caption text-grey">{{ getFileName(node.oldPath) }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  node: {
    type: Object,
    required: true
  },
  level: {
    type: Number,
    default: 0
  },
  searchQuery: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['toggle'])

function toggle() {
  if (props.node.isFolder) {
    emit('toggle', props.node.path)
  }
}

function getFileCount(node) {
  let count = 0

  function countFiles(n) {
    if (n.files) {
      count += n.files.length
    }
    if (n.children) {
      n.children.forEach(child => {
        if (child.isFolder) {
          countFiles(child)
        }
      })
    }
  }

  countFiles(node)
  return count
}

function getFileName(path) {
  const parts = path.split('/')
  return parts[parts.length - 1]
}

function getStatusIcon(status) {
  switch (status) {
    case 'Added':
      return 'mdi-plus-circle'
    case 'Modified':
      return 'mdi-pencil-circle'
    case 'Deleted':
      return 'mdi-minus-circle'
    case 'Renamed':
      return 'mdi-rename-box'
    case 'Copied':
      return 'mdi-content-copy'
    default:
      return 'mdi-file'
  }
}

function getStatusColor(status) {
  switch (status) {
    case 'Added':
      return 'success'
    case 'Modified':
      return 'warning'
    case 'Deleted':
      return 'error'
    case 'Renamed':
      return 'info'
    case 'Copied':
      return 'purple'
    default:
      return 'grey'
  }
}
</script>

<style scoped>
.tree-node {
  user-select: none;
}

.tree-node-item {
  display: flex;
  align-items: center;
  padding: 4px 8px;
  border-radius: 4px;
  transition: background-color 0.2s;
  min-height: 32px;
}

.folder-item {
  cursor: pointer;
  font-weight: 500;
}

.folder-item:hover {
  background-color: rgba(0, 0, 0, 0.04);
}

.v-theme--dark .folder-item:hover {
  background-color: rgba(255, 255, 255, 0.08);
}

.file-item {
  position: relative;
  padding-left: 20px;
}

.file-item:hover {
  background-color: rgba(0, 0, 0, 0.02);
}

.v-theme--dark .file-item:hover {
  background-color: rgba(255, 255, 255, 0.04);
}

.file-icon-placeholder {
  width: 16px;
  margin-right: 4px;
}

.folder-name {
  font-size: 0.9rem;
}

.file-name {
  font-size: 0.875rem;
  flex: 1;
}

.file-renamed {
  display: flex;
  align-items: center;
  gap: 4px;
}

.tree-children {
  margin-left: 0;
}
</style>
