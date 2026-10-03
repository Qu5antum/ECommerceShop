import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { categoryApi } from '../api/category'
import { productApi } from '../api/product'
import type { CategoryResponseDto } from '../types/category'
import type { ProductResponseDto } from '../types/product'
import Brand from '../components/Brand'

function ProductCard({ product }: { product: ProductResponseDto }) {
  const [imageUrl, setImageUrl] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true
    async function fetchImage() {
      try {
        const url = await productApi.getProductImage(product.id)
        if (isMounted) setImageUrl(url)
      } catch {
      }
    }
    fetchImage()
    return () => {
      isMounted = false
    }
  }, [product.id])

  return (
    <div className="product-card">
      <div className="product-image-wrapper">
        {imageUrl ? (
          <img src={imageUrl} alt={product.name} className="product-image" />
        ) : (
          <div className="product-image-placeholder">No image</div>
        )}
      </div>
      <div className="product-info">
        <h4>{product.name}</h4>
        <p className="product-desc">{product.description}</p>
        <div className="product-footer">
          <span className="product-price">${product.price}</span>
          <span className="product-stock">Stock: {product.stock}</span>
        </div>
        <Link to={`/product/${product.id}`} className="button button--dark button--small">
          View
        </Link>
      </div>
    </div>
  )
}

export default function MainPage() {
  const [categories, setCategories] = useState<CategoryResponseDto[]>([])
  const [products, setProducts] = useState<ProductResponseDto[]>([])
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [sortOrder, setSortOrder] = useState<'default' | 'asc' | 'desc'>('default')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    async function loadInitialData() {
      try {
        const [cats, prods] = await Promise.all([
          categoryApi.getAllCategories(),
          productApi.getProducts()
        ])
        setCategories(cats)
        setProducts(prods)
      } catch (err: any) {
        setError(err.message || 'Failed to load data')
      } finally {
        setLoading(false)
      }
    }
    loadInitialData()
  }, [])

  async function handleCategorySelect(categoryId: string | null) {
    setSelectedCategory(categoryId)
    setSearchQuery('')
    setSortOrder('default')
    setLoading(true)
    try {
      const data = categoryId 
        ? await productApi.getProductsByCategoryId(categoryId)
        : await productApi.getProducts()
      setProducts(data)
    } catch (err: any) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    if (!searchQuery.trim()) return
    setLoading(true)
    setSelectedCategory(null)
    try {
      let data: ProductResponseDto[]
      if (sortOrder === 'asc') {
        data = await productApi.searchProductAsc(searchQuery)
      } else if (sortOrder === 'desc') {
        data = await productApi.searchProductDesc(searchQuery)
      } else {
        data = await productApi.searchProduct(searchQuery)
      }
      setProducts(data)
    } catch (err: any) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleSortChange(order: 'default' | 'asc' | 'desc') {
    setSortOrder(order)
    setLoading(true)
    try {
      let data: ProductResponseDto[]
      if (searchQuery) {
        if (order === 'asc') data = await productApi.searchProductAsc(searchQuery)
        else if (order === 'desc') data = await productApi.searchProductDesc(searchQuery)
        else data = await productApi.searchProduct(searchQuery)
      } else {
        data = await productApi.getProducts()
        if (order === 'asc') data.sort((a, b) => a.price - b.price)
        if (order === 'desc') data.sort((a, b) => b.price - a.price)
      }
      setProducts(data)
    } catch (err: any) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  if (loading && categories.length === 0) return <div className="loading">Loading store...</div>

  return (
    <main className="home-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <form className="search-form" onSubmit={handleSearch}>
            <input 
              type="text" 
              placeholder="Search products..." 
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <button type="submit" className="button button--quiet">Search</button>
          </form>
          <div className="site-header__actions">
            <Link className="button button--quiet" to="/login">Log in</Link>
            <Link className="button button--dark" to="/register">Sign up</Link>
          </div>
        </div>
      </header>

      <section className="catalog-section">
        <div className="categories-sidebar">
          <h3>Categories</h3>
          <button 
            className={`category-btn ${!selectedCategory ? 'active' : ''}`}
            onClick={() => handleCategorySelect(null)}
          >
            All Products
          </button>
          {categories.map((cat) => (
            <button 
              key={cat.id} 
              className={`category-btn ${selectedCategory === cat.id ? 'active' : ''}`}
              onClick={() => handleCategorySelect(cat.id)}
            >
              {cat.title}
            </button>
          ))}
        </div>

        <div className="products-content">
          <div className="catalog-controls">
            <h2>{selectedCategory ? 'Category Products' : 'All Products'}</h2>
            <div className="sort-dropdown">
              <label>Sort by price: </label>
              <select value={sortOrder} onChange={(e) => handleSortChange(e.target.value as any)}>
                <option value="default">Default</option>
                <option value="asc">Price: Low to High</option>
                <option value="desc">Price: High to Low</option>
              </select>
            </div>
          </div>

          {error && <p className="error-message">{error}</p>}

          {loading ? (
            <p>Loading products...</p>
          ) : products.length === 0 ? (
            <p>No products found.</p>
          ) : (
            <div className="products-grid">
              {products.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          )}
        </div>
      </section>
    </main>
  )
}