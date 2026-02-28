import { 
  ChessPawn,
  ChessKnight,
  ChessQueen,
  ChessKing  
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
  { value: 0, icon: ChessPawn, label: 'Default', class: 'btn-priority-default', textClass: 'text-priority-default', borderColor: 'border-base-300' },
  { value: 1, icon: ChessKnight, label: 'Medium', class: 'btn-priority-medium', textClass: 'text-priority-medium', borderColor: 'border-slate-400/30' },
  { value: 2, icon: ChessQueen, label: 'High', class: 'btn-priority-high', textClass: 'text-priority-high', borderColor: 'border-blue-400/30' },
  { value: 3, icon: ChessKing, label: 'Top', class: 'btn-priority-top', textClass: 'text-priority-top', borderColor: 'border-amber-400/30' }
]
