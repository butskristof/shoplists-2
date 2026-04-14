import type { Shoplist } from "~/types/shoplist";
import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";
import { useToast } from "primevue/usetoast";

export const shoplistKeys = {
  all: ["shoplists"] as const,
  detail: (listId: string) => ["shoplists", listId] as const,
};

export function useShoplists() {
  const api = useApi();

  const { data, isPending, isError, suspense } = useQuery({
    queryKey: shoplistKeys.all,
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
      void queryClient.invalidateQueries({ queryKey: shoplistKeys.all, exact: true });
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

  const detailKey = shoplistKeys.detail(listId);

  const { data, isPending, error, suspense } = useQuery({
    queryKey: detailKey,
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
    sortedItems.value.filter(item => !item.isFulfilled),
  );

  const fulfilledItems = computed(() =>
    sortedItems.value.filter(item => item.isFulfilled),
  );

  const queryClient = useQueryClient();
  const toast = useToast();

  interface OptimisticContext { previousList: Shoplist | undefined }

  // Toggle fulfilled state
  const toggleMutation = useMutation<void, Error, { itemId: string; isFulfilled: boolean }, OptimisticContext>({
    mutationFn: async ({ itemId, isFulfilled }) => {
      const { error } = await api.PATCH(
        "/shoplists/{listId}/items/{itemId}/fulfilled",
        {
          params: { path: { listId, itemId } },
          body: { shoplistId: listId, itemId, isFulfilled },
        },
      );
      if (error)
        throw error;
    },
    onMutate: async ({ itemId, isFulfilled }) => {
      await queryClient.cancelQueries({ queryKey: detailKey });
      const previousList = queryClient.getQueryData<Shoplist>(detailKey);

      queryClient.setQueryData<Shoplist>(detailKey, (old) => {
        if (!old)
          return old;
        return {
          ...old,
          items: old.items.map(item =>
            item.id === itemId ? { ...item, isFulfilled } : item,
          ),
        };
      });

      return { previousList };
    },
    onError: (_err, _vars, context) => {
      if (context?.previousList)
        queryClient.setQueryData<Shoplist>(detailKey, context.previousList);
      toast.add({ severity: "error", summary: "Failed to update item", life: 3000 });
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey });
      void queryClient.invalidateQueries({ queryKey: shoplistKeys.all, exact: true });
    },
  });

  async function toggleItem(itemId: string): Promise<boolean> {
    const item = data.value?.items.find(i => i.id === itemId);
    if (!item)
      return false;
    try {
      await toggleMutation.mutateAsync({ itemId, isFulfilled: !item.isFulfilled });
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
      void queryClient.invalidateQueries({ queryKey: detailKey });
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
  const deleteMutation = useMutation<void, Error, string, OptimisticContext>({
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
    onMutate: async (itemId) => {
      await queryClient.cancelQueries({ queryKey: detailKey });
      const previousList = queryClient.getQueryData<Shoplist>(detailKey);

      queryClient.setQueryData<Shoplist>(detailKey, (old) => {
        if (!old)
          return old;
        return {
          ...old,
          items: old.items.filter(item => item.id !== itemId),
        };
      });

      return { previousList };
    },
    onError: (_err, _vars, context) => {
      if (context?.previousList)
        queryClient.setQueryData<Shoplist>(detailKey, context.previousList);
      toast.add({ severity: "error", summary: "Failed to delete item", life: 3000 });
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey });
      void queryClient.invalidateQueries({ queryKey: shoplistKeys.all, exact: true });
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
  const updateNameMutation = useMutation<void, Error, { itemId: string; name: string }, OptimisticContext>({
    mutationFn: async ({ itemId, name }) => {
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
    onMutate: async ({ itemId, name }) => {
      await queryClient.cancelQueries({ queryKey: detailKey });
      const previousList = queryClient.getQueryData<Shoplist>(detailKey);

      queryClient.setQueryData<Shoplist>(detailKey, (old) => {
        if (!old)
          return old;
        return {
          ...old,
          items: old.items.map(item =>
            item.id === itemId ? { ...item, name } : item,
          ),
        };
      });

      return { previousList };
    },
    onError: (_err, _vars, context) => {
      if (context?.previousList)
        queryClient.setQueryData<Shoplist>(detailKey, context.previousList);
      toast.add({ severity: "error", summary: "Failed to rename item", life: 3000 });
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey });
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
  const reorderMutation = useMutation<void, Error, { itemId: string; position: number }, OptimisticContext>({
    mutationFn: async ({ itemId, position }) => {
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
    onMutate: async ({ itemId, position: newPosition }) => {
      await queryClient.cancelQueries({ queryKey: detailKey });
      const previousList = queryClient.getQueryData<Shoplist>(detailKey);

      queryClient.setQueryData<Shoplist>(detailKey, (old) => {
        if (!old)
          return old;

        const movingItem = old.items.find(i => i.id === itemId);
        if (!movingItem)
          return old;

        const oldPosition = Number(movingItem.position);

        return {
          ...old,
          items: old.items.map((item) => {
            if (item.id === itemId)
              return { ...item, position: newPosition };

            const pos = Number(item.position);

            // Moving down (e.g., position 2 -> 5):
            // Items in (oldPos, newPos] shift up by 1
            if (oldPosition < newPosition && pos > oldPosition && pos <= newPosition)
              return { ...item, position: pos - 1 };

            // Moving up (e.g., position 5 -> 2):
            // Items in [newPos, oldPos) shift down by 1
            if (oldPosition > newPosition && pos >= newPosition && pos < oldPosition)
              return { ...item, position: pos + 1 };

            return item;
          }),
        };
      });

      return { previousList };
    },
    onError: (_err, _vars, context) => {
      if (context?.previousList)
        queryClient.setQueryData<Shoplist>(detailKey, context.previousList);
      toast.add({ severity: "error", summary: "Failed to reorder item", life: 3000 });
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey });
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

  // Update list name
  const updateListNameMutation = useMutation<void, Error, string, OptimisticContext>({
    mutationFn: async (name: string) => {
      const { error } = await api.PUT(
        "/shoplists/{id}",
        {
          params: { path: { id: listId } },
          body: { id: listId, name },
        },
      );
      if (error)
        throw error;
    },
    onMutate: async (name) => {
      await queryClient.cancelQueries({ queryKey: detailKey });
      const previousList = queryClient.getQueryData<Shoplist>(detailKey);

      queryClient.setQueryData<Shoplist>(detailKey, (old) => {
        if (!old)
          return old;
        return { ...old, name };
      });

      return { previousList };
    },
    onError: (_err, _vars, context) => {
      if (context?.previousList)
        queryClient.setQueryData<Shoplist>(detailKey, context.previousList);
      toast.add({ severity: "error", summary: "Failed to rename list", life: 3000 });
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey });
      void queryClient.invalidateQueries({ queryKey: shoplistKeys.all, exact: true });
    },
  });

  async function updateListName(newName: string): Promise<boolean> {
    try {
      await updateListNameMutation.mutateAsync(newName);
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
    fulfilledItems,
    toggleItem,
    addItem,
    deleteItem,
    updateItemName,
    updateListName,
    reorderItem,
  };
}
