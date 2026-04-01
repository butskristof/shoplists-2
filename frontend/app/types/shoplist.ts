export interface ShoplistItem {
  id: string;
  name: string;
  done: boolean;
  position: number;
}

export interface Shoplist {
  id: string;
  name: string;
  items: ShoplistItem[];
}

export interface ShoplistSummary {
  id: string;
  name: string;
  itemCount: number;
  doneCount: number;
}
