import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";

export function useShoplists() {
  const api = useApi();

  const { data, isPending, isError, suspense } = useQuery({
    queryKey: ["shoplists"],
    queryFn: async () => {
      const { data, error } = await api.GET("/shoplists");
      if (error)
        throw error;
      return data;
    },
  });

  onServerPrefetch(async () => {
    await suspense();
  });

  const queryClient = useQueryClient();

  const createListMutation = useMutation({
    mutationFn: async (name: string) => {
      const { data, error } = await api.POST("/shoplists", {
        body: { name },
      });
      if (error)
        throw error;
      return data;
    },
    onSuccess: () => {
      // `void` is intentional: we fire-and-forget the invalidation so the
      // mutation settles as soon as the HTTP call succeeds. If we returned
      // (or awaited) the promise, Vue Query would keep the mutation in its
      // pending state until the triggered refetch completes — making the UI
      // feel slower for no benefit here. The refetch still happens in the
      // background and reconciles the cache shortly after. Same pattern is
      // applied to every onSuccess in this file.
      void queryClient.invalidateQueries({ queryKey: ["shoplists"], exact: true });
    },
  });

  async function createList(name: string): Promise<string | undefined> {
    try {
      const result = await createListMutation.mutateAsync(name);
      return result.id;
    }
    catch {
      return undefined;
    }
  }

  return { lists: data, isPending, isError, createList };
}

export function useShoplist(listId: string) {
  const api = useApi();

  const { data, isPending, error, suspense } = useQuery({
    queryKey: ["shoplists", listId],
    queryFn: async () => {
      const { data, error } = await api.GET("/shoplists/{id}", {
        params: { path: { id: listId } },
      });
      if (error)
        throw error;
      return data;
    },
  });

  onServerPrefetch(async () => {
    await suspense();
  });

  const sortedItems = computed(() =>
    (data.value?.items ?? []).toSorted(
      (a, b) => Number(a.position) - Number(b.position),
    ),
  );

  const itemsToGet = computed(() =>
    sortedItems.value.filter(item => !item.isChecked),
  );

  const doneItems = computed(() =>
    sortedItems.value.filter(item => item.isChecked),
  );

  const queryClient = useQueryClient();

  // Toggle checked state
  const toggleMutation = useMutation({
    mutationFn: async (itemId: string) => {
      const item = data.value?.items.find(i => i.id === itemId);
      if (!item)
        throw new Error("Item not found");
      const { error } = await api.PATCH(
        "/shoplists/{listId}/items/{itemId}/checked",
        {
          params: { path: { listId, itemId } },
          body: {
            shoplistId: listId,
            itemId,
            isChecked: !item.isChecked,
          },
        },
      );
      if (error)
        throw error;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["shoplists", listId] });
      void queryClient.invalidateQueries({ queryKey: ["shoplists"], exact: true });
    },
  });

  async function toggleItem(itemId: string): Promise<boolean> {
    try {
      await toggleMutation.mutateAsync(itemId);
      return true;
    }
    catch {
      return false;
    }
  }

  // Add item
  const addMutation = useMutation({
    mutationFn: async (name: string) => {
      const { error } = await api.POST(
        "/shoplists/{listId}/items",
        {
          params: { path: { listId } },
          body: { shoplistId: listId, name },
        },
      );
      if (error)
        throw error;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["shoplists", listId] });
    },
  });

  async function addItem(name: string): Promise<boolean> {
    try {
      await addMutation.mutateAsync(name);
      return true;
    }
    catch {
      return false;
    }
  }

  // Delete item
  const deleteMutation = useMutation({
    mutationFn: async (itemId: string) => {
      const { error } = await api.DELETE(
        "/shoplists/{listId}/items/{itemId}",
        {
          params: { path: { listId, itemId } },
        },
      );
      if (error)
        throw error;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["shoplists", listId] });
      void queryClient.invalidateQueries({ queryKey: ["shoplists"], exact: true });
    },
  });

  async function deleteItem(itemId: string): Promise<boolean> {
    try {
      await deleteMutation.mutateAsync(itemId);
      return true;
    }
    catch {
      return false;
    }
  }

  // Update item name
  const updateNameMutation = useMutation({
    mutationFn: async ({ itemId, name }: { itemId: string; name: string }) => {
      const { error } = await api.PUT(
        "/shoplists/{listId}/items/{itemId}",
        {
          params: { path: { listId, itemId } },
          body: { shoplistId: listId, itemId, name },
        },
      );
      if (error)
        throw error;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["shoplists", listId] });
    },
  });

  async function updateItemName(
    itemId: string,
    newName: string,
  ): Promise<boolean> {
    try {
      await updateNameMutation.mutateAsync({ itemId, name: newName });
      return true;
    }
    catch {
      return false;
    }
  }

  // Reorder item
  const reorderMutation = useMutation({
    mutationFn: async ({
      itemId,
      position,
    }: {
      itemId: string;
      position: number;
    }) => {
      const { error } = await api.PATCH(
        "/shoplists/{listId}/items/{itemId}/position",
        {
          params: { path: { listId, itemId } },
          body: { shoplistId: listId, itemId, position },
        },
      );
      if (error)
        throw error;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["shoplists", listId] });
    },
  });

  async function reorderItem(
    itemId: string,
    newPosition: number,
  ): Promise<boolean> {
    try {
      await reorderMutation.mutateAsync({
        itemId,
        position: newPosition,
      });
      return true;
    }
    catch {
      return false;
    }
  }

  return {
    list: data,
    isLoading: isPending,
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
