import type { Shoplist, ShoplistItem } from "~/types/shoplist";

const MOCK_DATA: Record<string, Shoplist> = {
  "weekly-groceries": {
    id: "weekly-groceries",
    name: "Weekly groceries with a very long name for the list",
    items: [
      { id: "1", name: "Bananas", done: false, position: 1 },
      { id: "2", name: "Whole milk", done: false, position: 2 },
      { id: "3", name: "Sourdough bread", done: false, position: 3 },
      { id: "4", name: "Eggs", done: false, position: 4 },
      { id: "5", name: "Olive oil", done: true, position: 5 },
      { id: "6", name: "Cherry tomatoes", done: false, position: 6 },
      { id: "7", name: "Chicken thighs", done: false, position: 7 },
      { id: "8", name: "Greek yogurt", done: true, position: 8 },
    ],
  },
};

function findItem(items: ShoplistItem[], itemId: string): ShoplistItem | undefined {
  return items.find(i => i.id === itemId);
}

export function useShoplist(listId: string) {
  const list = ref<Shoplist | null>(MOCK_DATA[listId] ?? null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const sortedItems = computed(() =>
    (list.value?.items ?? []).toSorted((a, b) => a.position - b.position),
  );

  const itemsToGet = computed(() =>
    sortedItems.value.filter(item => !item.done),
  );

  const doneItems = computed(() =>
    sortedItems.value.filter(item => item.done),
  );

  async function toggleItem(itemId: string) {
    if (!list.value)
      return;
    const item = findItem(list.value.items, itemId);
    if (item)
      item.done = !item.done;
  }

  async function addItem(name: string) {
    if (!list.value)
      return;
    const maxPosition = list.value.items.length > 0
      ? Math.max(...list.value.items.map(i => i.position))
      : 0;
    list.value.items.push({
      id: crypto.randomUUID(),
      name,
      done: false,
      position: maxPosition + 1,
    });
  }

  async function deleteItem(itemId: string) {
    if (!list.value)
      return;
    list.value.items = list.value.items.filter(i => i.id !== itemId);
  }

  async function updateItemName(itemId: string, newName: string) {
    if (!list.value)
      return;
    const item = findItem(list.value.items, itemId);
    if (item)
      item.name = newName;
  }

  async function reorderItem(itemId: string, newPosition: number) {
    if (!list.value)
      return;
    const item = findItem(list.value.items, itemId);
    if (!item)
      return;

    const oldPosition = item.position;
    if (oldPosition === newPosition)
      return;

    for (const other of list.value.items) {
      if (other.id === itemId)
        continue;
      if (oldPosition < newPosition) {
        // Moving down: shift items in between up
        if (other.position > oldPosition && other.position <= newPosition)
          other.position--;
      }
      else {
        // Moving up: shift items in between down
        if (other.position >= newPosition && other.position < oldPosition)
          other.position++;
      }
    }
    item.position = newPosition;
  }

  return {
    list,
    isLoading,
    error,
    sortedItems,
    itemsToGet,
    doneItems,
    toggleItem,
    addItem,
    deleteItem,
    updateItemName,
    reorderItem,
  };
}
