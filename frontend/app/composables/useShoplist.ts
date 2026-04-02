import type { Shoplist, ShoplistItem, ShoplistSummary } from "~/types/shoplist";

const MOCK_DATA: Record<string, Shoplist> = {
  "weekly-groceries": {
    id: "weekly-groceries",
    name: "Weekly groceries with a very long name for the list which really takes up all the space",
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
  "birthday-party": {
    id: "birthday-party",
    name: "Birthday party supplies",
    items: [
      { id: "bp-1", name: "Cake mix", done: true, position: 1 },
      { id: "bp-2", name: "Candles", done: true, position: 2 },
      { id: "bp-3", name: "Balloons", done: true, position: 3 },
      { id: "bp-4", name: "Paper plates", done: true, position: 4 },
      { id: "bp-5", name: "Napkins", done: true, position: 5 },
    ],
  },
  "weeknight-pasta": {
    id: "weeknight-pasta",
    name: "Weeknight pasta",
    items: [
      { id: "wp-1", name: "Spaghetti", done: false, position: 1 },
      { id: "wp-2", name: "Pancetta", done: false, position: 2 },
      { id: "wp-3", name: "Parmesan", done: true, position: 3 },
    ],
  },
  "empty-list": {
    id: "empty-list",
    name: "New list",
    items: [],
  },
};

function findItem(items: ShoplistItem[], itemId: string): ShoplistItem | undefined {
  return items.find(i => i.id === itemId);
}

export function useShoplists() {
  const lists = computed<ShoplistSummary[]>(() =>
    Object.values(MOCK_DATA).map(list => ({
      id: list.id,
      name: list.name,
      itemCount: list.items.length,
      doneCount: list.items.filter(i => i.done).length,
    })),
  );

  async function createList(name: string) {
    const id = crypto.randomUUID();
    MOCK_DATA[id] = {
      id,
      name,
      items: [],
    };
    return id;
  }

  return { lists, createList };
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

  async function toggleItem(itemId: string): Promise<boolean> {
    if (!list.value)
      return false;
    const item = findItem(list.value.items, itemId);
    if (!item)
      return false;
    try {
      item.done = !item.done;
      // Future: API call here — revert item.done on failure
      return true;
    }
    catch {
      // item.done = !item.done; // revert optimistic update
      return false;
    }
  }

  async function addItem(name: string): Promise<boolean> {
    if (!list.value)
      return false;
    try {
      const maxPosition = list.value.items.length > 0
        ? Math.max(...list.value.items.map(i => i.position))
        : 0;
      list.value.items.push({
        id: crypto.randomUUID(),
        name,
        done: false,
        position: maxPosition + 1,
      });
      // Future: API call here — remove item on failure
      return true;
    }
    catch {
      return false;
    }
  }

  async function deleteItem(itemId: string): Promise<boolean> {
    if (!list.value)
      return false;
    const previousItems = [...list.value.items];
    try {
      list.value.items = list.value.items.filter(i => i.id !== itemId);
      // Future: API call here — restore previousItems on failure
      return true;
    }
    catch {
      list.value.items = previousItems; // revert optimistic update
      return false;
    }
  }

  async function updateItemName(itemId: string, newName: string): Promise<boolean> {
    if (!list.value)
      return false;
    const item = findItem(list.value.items, itemId);
    if (!item)
      return false;
    const previousName = item.name;
    try {
      item.name = newName;
      // Future: API call here — revert to previousName on failure
      return true;
    }
    catch {
      item.name = previousName; // revert optimistic update
      return false;
    }
  }

  async function reorderItem(itemId: string, newPosition: number): Promise<boolean> {
    if (!list.value)
      return false;
    const item = findItem(list.value.items, itemId);
    if (!item)
      return false;

    const oldPosition = item.position;
    if (oldPosition === newPosition)
      return true;

    const previousPositions = list.value.items.map(i => ({ id: i.id, position: i.position }));
    try {
      for (const other of list.value.items) {
        if (other.id === itemId)
          continue;
        if (oldPosition < newPosition) {
          if (other.position > oldPosition && other.position <= newPosition)
            other.position--;
        }
        else {
          if (other.position >= newPosition && other.position < oldPosition)
            other.position++;
        }
      }
      item.position = newPosition;
      // Future: API call here — restore previousPositions on failure
      return true;
    }
    catch {
      // revert all positions
      for (const prev of previousPositions) {
        const i = findItem(list.value.items, prev.id);
        if (i)
          i.position = prev.position;
      }
      return false;
    }
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
