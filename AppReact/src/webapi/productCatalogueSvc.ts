import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class ProductCatalogueService {
  
  async getProducts(categoryId?: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/GetProducts?categoryId=${categoryId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get products');
    return response.json();
  }

  async getProduct(productId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/GetProduct?productId=${productId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get product');
    return response.json();
  }

  async createProduct(product: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/CreateProduct`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(product)
    });
    if (!response.ok) throw new Error('Failed to create product');
    return response.json();
  }

  async updateProduct(product: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/UpdateProduct`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(product)
    });
    if (!response.ok) throw new Error('Failed to update product');
    return response.json();
  }

  async deleteProduct(productId: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/DeleteProduct?productId=${productId}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete product');
  }

  async getCategories(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/GetCategories`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get categories');
    return response.json();
  }

  async createCategory(category: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/CreateCategory`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(category)
    });
    if (!response.ok) throw new Error('Failed to create category');
    return response.json();
  }

  async updateCategory(category: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/UpdateCategory`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(category)
    });
    if (!response.ok) throw new Error('Failed to update category');
    return response.json();
  }

  async deleteCategory(categoryId: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/DeleteCategory?categoryId=${categoryId}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete category');
  }

  async searchProducts(query: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProductCatalogue/SearchProducts?query=${encodeURIComponent(query)}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to search products');
    return response.json();
  }
}

export const productCatalogueService = new ProductCatalogueService(); 