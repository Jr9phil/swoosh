import type { Component } from 'vue';
import { 
    Bookmark,
    Brain,
    Lightbulb,
    Milestone,
    PencilRuler,
    Wrench,
    Crown,
    Flame,
    GraduationCap,
    Zap,
    Trophy,
    Brush,
    Sprout,
    Balloon,
    Coffee,
    House,
    ShoppingCart,
    Dumbbell,
    Heart,
    Wallet,
    Compass,
    Globe,
    Luggage,
    Lock
} from 'lucide-vue-next';

export interface TaskIcon {
    value: number
    icon: Component
    color: string
}

export const TASK_ICONS: TaskIcon[] = [
    { value: 0, icon: Bookmark, color: 'text-red-400' },
    { value: 1, icon: Brain, color: 'text-purple-400' },
    { value: 2, icon: Lightbulb, color: 'text-yellow-400' },
    { value: 3, icon: Milestone, color: 'text-blue-400' },
    { value: 4, icon: PencilRuler, color: 'text-teal-400' },
    { value: 5, icon: Wrench, color: 'text-slate-400' },
    { value: 6, icon: Crown, color: 'text-yellow-500' },
    { value: 7, icon: Flame, color: 'text-orange-500' },
    { value: 8, icon: GraduationCap, color: 'text-blue-500' },
    { value: 9, icon: Zap, color: 'text-yellow-400' },
    { value: 10, icon: Trophy, color: 'text-amber-500' },
    { value: 11, icon: Brush, color: 'text-pink-400' },
    { value: 12, icon: Sprout, color: 'text-green-500' },
    { value: 13, icon: Balloon, color: 'text-red-400' },
    { value: 14, icon: Coffee, color: 'text-amber-600' },
    { value: 15, icon: House, color: 'text-emerald-500' },
    { value: 16, icon: ShoppingCart, color: 'text-cyan-500' },
    { value: 17, icon: Dumbbell, color: 'text-slate-500' },
    { value: 18, icon: Heart, color: 'text-rose-500' },
    { value: 19, icon: Wallet, color: 'text-green-400' },
    { value: 20, icon: Compass, color: 'text-sky-500' },
    { value: 21, icon: Globe, color: 'text-blue-400' },
    { value: 22, icon: Luggage, color: 'text-violet-400' },
    { value: 23, icon: Lock, color: 'text-slate-400' },
]