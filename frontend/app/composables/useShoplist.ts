import type { components } from "~/generated/api";
import type { Shoplist } from "~/types/shoplist";
import { useMutation, useQuery, useQueryClient } from "@tanstack/vue-query";
import { useToast } from "primevue/usetoast";

type ProblemDetails = components["schemas"]["ProblemDetails"];

export const shoplistKeys = {
  all: ["shoplists"] as const,
  detail: (listId: string) => ["shoplists", listId] as const,
};

export function useShoplists() {
  const api = useApi();
  const toast = useToast();

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
    onError: () => {
      toast.add({ severity: "error", summary: "Failed to create list", life: 3000 });
    },
  });

  const createList = (name: string) =>
    createListMutation.mutateAsync(name).then(r => r.id);

  return {
    lists: data,
    isPending,
    isError,
    createList,
    isCreatingList: createListMutation.isPending,
  };
}

export function useShoplist(listId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const api = useApi();

  const detailKey = shoplistKeys.detail(listId);

  const { data, isPending, isError, error, suspense } = useQuery<Shoplist, ProblemDetails>({
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
  const isNotFound = computed(() => isError.value && Number(error.value?.status) === 404);

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

  const toggleItem = (itemId: string) => {
    const item = data.value?.items.find(i => i.id === itemId);
    if (!item)
      throw new Error("Item to toggle not found");
    toggleMutation.mutate({ itemId, isFulfilled: !item.isFulfilled });
  };

  // Add item
  const addItemMutation = useMutation({
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
    onError: () => {
      toast.add({ severity: "error", summary: "Failed to add item", life: 3000 });
    },
  });

  const addItem = (name: string) => void addItemMutation.mutate(name);

  // Delete item
  const deleteItemMutation = useMutation<void, Error, string, OptimisticContext>({
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
    onSuccess: () => {
      toast.add({ severity: "info", summary: "Item removed", life: 2000 });
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

  const deleteItem = (itemId: string) => void deleteItemMutation.mutate(itemId);

  // Update item name
  const updateItemNameMutation = useMutation<void, Error, { itemId: string; name: string }, OptimisticContext>({
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

  const updateItemName = (itemId: string, name: string) =>
    void updateItemNameMutation.mutate({ itemId, name });

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

  const reorderItem = (itemId: string, position: number) =>
    void reorderMutation.mutate({ itemId, position });

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
    onSuccess: () => {
      toast.add({ severity: "info", summary: "New list name saved", life: 2000 });
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

  const updateListName = (name: string) => void updateListNameMutation.mutate(name);

  return {
    list: data,
    isPending,
    isError,
    isNotFound,
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
