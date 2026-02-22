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
  style: string
}

export const PRIORITIES: Priority[] = [
  { value: 0, icon: ChessPawn, label: 'Default', style: 'btn-ghost opacity-0 group-hover:opacity-50' },
  { value: 1, icon: ChessKnight, label: 'Medium', style: 'btn-soft text-slate-400 bg-slate-400/10' },
  { value: 2, icon: ChessQueen, label: 'High', style: 'btn-soft text-blue-400 bg-blue-400/10' },
  { value: 3, icon: ChessKing, label: 'Top', style: 'btn-soft text-amber-400 bg-amber-400/10' }
]
