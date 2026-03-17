import {
  Dot,
  ChevronDown,
  Minus,
} from 'lucide-vue-next'
import TriangleUpIcon from '../components/TriangleUpIcon.vue'
import type { Component } from 'vue'

export interface Priority {
  value: number
  icon: Component
  label: string
  shortLabel: string
  class: string
  textClass: string
  borderColor: string
}

export const PRIORITIES: Priority[] = [
  { value: 0, icon: Dot, label: 'No Priority', shortLabel: 'None', class: 'none', textClass: 'text-priority-default', borderColor: '' },
  { value: 1, icon: ChevronDown, label: 'Low Priority', shortLabel: 'Low', class: 'low', textClass: 'text-priority-low', borderColor: 'border-priority-low' },
  { value: 2, icon: Minus, label: 'Medium Priority', shortLabel: 'Med', class: 'med', textClass: 'text-priority-medium', borderColor: 'border-priority-medium' },
  { value: 3, icon: TriangleUpIcon, label: 'High Priority', shortLabel: 'High', class: 'high', textClass: 'text-priority-high', borderColor: 'border-priority-high' }
]
