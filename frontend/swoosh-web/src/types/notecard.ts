export interface NoteCard {
    id: string
    title: string
    body: string | null
    positionX: number
    positionY: number
    createdAt: string
    modified: string
}

export interface CreateNoteCard {
    title: string
    body?: string | null
    positionX: number
    positionY: number
}
