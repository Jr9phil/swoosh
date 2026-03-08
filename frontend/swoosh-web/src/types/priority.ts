import { 
  Circle,
  ChevronDown,
  Minus,
  Triangle
} from 'lucide-vue-next'
import type { Component } from 'vue'

export interface Priority {
  value: number
  icon: Component
  label: string
  class: string
  textClass: string
  borderColor: string
}

export const PRIORITIES: Priority[] = [
  { value: 0, icon: Circle, label: 'No Priority', class: 'none', textClass: 'text-priority-default', borderColor: '' },
  { value: 1, icon: ChevronDown, label: 'Low Priority', class: 'low', textClass: 'text-priority-low', borderColor: 'border-priority-low' },
  { value: 2, icon: Minus, label: 'Medium Priority', class: 'med', textClass: 'text-priority-medium', borderColor: 'border-priority-medium' },
  { value: 3, icon: Triangle, label: 'High Priority', class: 'high', textClass: 'text-priority-high', borderColor: 'border-priority-high' }
]
