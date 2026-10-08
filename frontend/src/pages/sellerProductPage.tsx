import { useEffect, useState, type FormEvent, useRef } from 'react'
import { Link } from 'react-router-dom'
import { sellerProfileApi } from '../api/seller'
import { productApi } from '../api/product'
import { categoryApi } from '../api/category'
import type { 
  ProductResponseDto, 
  ProductCreateData, 
  ProductUpdateData 
} from '../types/product'
import type { CategoryResponseDto } from '../types/category'
import Brand from '../components/Brand'
import '../style/sellerProductsPage.css'

export default function SellerProductsPage() {
  const [sellerId, setSellerId] = useState<string | null>(null)
  const [products, setProducts] = useState<ProductResponseDto[]>([])
  const [outOfStockProducts, setOutOfStockProducts] = useState<ProductResponseDto[]>([])
  const [categories, setCategories] = useState<CategoryResponseDto[]>([])
  const [productImages, setProductImages] = useState<Record<string, string>>({})
  
  const [activeTab, setActiveTab] = useState<'all' | 'outofstock'>('all')

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<ProductResponseDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [categoryId, setCategoryId] = useState('')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [price, setPrice] = useState('')
  const [sku, setSku] = useState('')
  const [stock, setStock] = useState('')
  const [imageFile, setImageFile] = useState<File | null>(null)

  const [editCategoryId, setEditCategoryId] = useState('')
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editPrice, setEditPrice] = useState('')
  const [editSku, setEditSku] = useState('')
  const [editStock, setEditStock] = useState('')
  const [editImageFile, setEditImageFile] = useState<File | null>(null)

  const fileInputRef = useRef<HTMLInputElement>(null)
  const editFileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    async function initPageData() {
      try {
        setLoading(true)
        setError('')

        const [profile, categoriesData] = await Promise.all([
          sellerProfileApi.getCurrentUserSellerProfile(),
          categoryApi.getAllCategories().catch(() => [])
        ])

        if (!profile || !profile.id) {
          throw new Error('Seller profile not found for current user')
        }

        setSellerId(profile.id)
        setCategories(categoriesData)

        if (categoriesData.length > 0) {
          setCategoryId(categoriesData[0].id)
        }

        await fetchAllData(profile.id)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to initialize seller page')
        setLoading(false)
      }
    }

    initPageData()
  }, [])

  async function fetchAllData(targetSellerId: string) {
    try {
      setLoading(true)
      const [allProductsData, outOfStockData] = await Promise.all([
        productApi.getProductsOfSeller(targetSellerId),
        productApi.getProductsOutOfStock().catch(() => [])
      ])

      const sellerOutOfStock = outOfStockData.filter(
        (p: ProductResponseDto) => p.sellerProfileId === targetSellerId || allProductsData.some(ap => ap.id === p.id && ap.stock === 0)
      )

      setProducts(allProductsData)
      setOutOfStockProducts(sellerOutOfStock)

      const allIds = Array.from(new Set([...allProductsData.map(p => p.id), ...sellerOutOfStock.map(p => p.id)]))

      if (allIds.length > 0) {
        const imagePromises = allIds.map(async (productId) => {
          try {
            const url = await productApi.getProductImage(productId)
            return { id: productId, url }
          } catch {
            return { id: productId, url: null }
          }
        })

        const imagesResults = await Promise.all(imagePromises)
        const imagesMap: Record<string, string> = {}
        imagesResults.forEach((item) => {
          if (item.url) imagesMap[item.id] = item.url
        })
        setProductImages(imagesMap)
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load seller products')
    } finally {
      setLoading(false)
    }
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!sellerId) return

    try {
      setSubmitting(true)
      setError('')

      const createData: ProductCreateData = {
        categoryId,
        name,
        description,
        price: parseFloat(price) || 0,
        sku,
        stock: parseInt(stock) || 0,
        image: imageFile,
      }

      await productApi.createProduct(createData)
      setSuccessMessage('Product successfully created!')
      setIsCreateOpen(false)
      resetCreateForm()
      await fetchAllData(sellerId) 
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to create product')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleUpdate(e: FormEvent) {
    e.preventDefault()
    if (!editingProduct || !sellerId) return

    try {
      setSubmitting(true)
      setError('')

      const updateData: ProductUpdateData = {
        categoryId: editCategoryId || undefined,
        name: editName || undefined,
        description: editDescription || undefined,
        price: editPrice ? parseFloat(editPrice) : undefined,
        sku: editSku || undefined,
        stock: editStock ? parseInt(editStock) : undefined,
        image: editImageFile,
      }

      await productApi.updateProduct(editingProduct.id, updateData)
      setSuccessMessage('Product successfully updated!')
      setEditingProduct(null)
      await fetchAllData(sellerId) 
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update product')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete(productId: string) {
    if (!window.confirm('Are you sure you want to delete this product?')) return
    if (!sellerId) return

    try {
      setError('')
      await productApi.deleteProduct(productId)
      setSuccessMessage('Product deleted successfully')
      setProducts(products.filter((p) => p.id !== productId))
      setOutOfStockProducts(outOfStockProducts.filter((p) => p.id !== productId))
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to delete product')
    }
  }

  function openEditModal(product: ProductResponseDto) {
    setEditingProduct(product)
    setEditCategoryId(product.categoryId)
    setEditName(product.name)
    setEditDescription(product.description)
    setEditPrice(product.price.toString())
    setEditSku(product.sku)
    setEditStock(product.stock.toString())
    setEditImageFile(null)
  }

  function resetCreateForm() {
    setCategoryId(categories.length > 0 ? categories[0].id : '')
    setName('')
    setDescription('')
    setPrice('')
    setSku('')
    setStock('')
    setImageFile(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const displayedProducts = activeTab === 'all' ? products : outOfStockProducts

  if (loading) return <div className="loading">Loading products...</div>

  return (
    <main className="seller-products-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/seller/profile" className="button button--quiet">← Back to Dashboard</Link>
        </div>
      </header>

      <div className="products-container">
        <div className="products-header-row">
          <h1>Manage Products</h1>
          <button className="button button--dark" onClick={() => setIsCreateOpen(true)}>
            + Add New Product
          </button>
        </div>

        <div className="products-tabs-row" style={{ display: 'flex', gap: '10px', marginBottom: '20px' }}>
          <button 
            className={`button ${activeTab === 'all' ? 'button--dark' : 'button--quiet'}`}
            onClick={() => setActiveTab('all')}
          >
            All Products ({products.length})
          </button>
          <button 
            className={`button ${activeTab === 'outofstock' ? 'button--dark' : 'button--quiet'}`}
            onClick={() => setActiveTab('outofstock')}
          >
            Out of Stock ({outOfStockProducts.length})
          </button>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {displayedProducts.length === 0 ? (
          <div className="no-products-card">
            <p>
              {activeTab === 'all' 
                ? "You haven't added any products yet." 
                : "Great! You have no out-of-stock products."}
            </p>
          </div>
        ) : (
          <div className="seller-products-grid">
            {displayedProducts.map((product) => {
              const imgUrl = productImages[product.id]
              return (
                <div key={product.id} className="seller-product-card">
                  <div className="product-card-image-box">
                    {imgUrl ? (
                      <img src={imgUrl} alt={product.name} />
                    ) : (
                      <span className="no-image-placeholder">No Image</span>
                    )}
                    {product.stock === 0 && (
                      <span className="out-of-stock-badge">Out of Stock</span>
                    )}
                  </div>
                  <div className="product-card-body">
                    <h3>{product.name}</h3>
                    <p className="product-sku">SKU: {product.sku}</p>
                    <p className="product-desc">{product.description}</p>
                    <div className="product-meta-row">
                      <span className="product-price">${product.price.toFixed(2)}</span>
                      <span className={`product-stock ${product.stock === 0 ? 'text-danger' : ''}`}>
                        Stock: {product.stock}
                      </span>
                    </div>
                  </div>
                  <div className="product-card-actions">
                    <button className="button button--quiet" onClick={() => openEditModal(product)}>
                      Edit
                    </button>
                    <button className="button button--danger" onClick={() => handleDelete(product.id)}>
                      Delete
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        )}

        {isCreateOpen && (
          <div className="modal-overlay">
            <div className="modal-content">
              <h2>Create New Product</h2>
              <form onSubmit={handleCreate} className="product-form">
                <label className="form-field">
                  <span>Category *</span>
                  <select 
                    value={categoryId} 
                    onChange={(e) => setCategoryId(e.target.value)} 
                    required
                  >
                    <option value="" disabled>Select category</option>
                    {categories.map((cat) => (
                      <option key={cat.id} value={cat.id}>
                        {cat.title}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="form-field">
                  <span>Product Name *</span>
                  <input type="text" value={name} onChange={(e) => setName(e.target.value)} required />
                </label>
                <label className="form-field">
                  <span>Description *</span>
                  <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} required />
                </label>
                <div className="form-row-2">
                  <label className="form-field">
                    <span>Price ($) *</span>
                    <input type="number" step="0.01" value={price} onChange={(e) => setPrice(e.target.value)} required />
                  </label>
                  <label className="form-field">
                    <span>Stock *</span>
                    <input type="number" value={stock} onChange={(e) => setStock(e.target.value)} required />
                  </label>
                </div>
                <label className="form-field">
                  <span>SKU *</span>
                  <input type="text" value={sku} onChange={(e) => setSku(e.target.value)} required />
                </label>
                <label className="form-field">
                  <span>Product Image</span>
                  <input type="file" ref={fileInputRef} accept="image/*" onChange={(e) => setImageFile(e.target.files?.[0] || null)} />
                </label>
                <div className="modal-actions">
                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Creating...' : 'Create Product'}
                  </button>
                  <button type="button" className="button button--quiet" onClick={() => setIsCreateOpen(false)}>
                    Cancel
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {editingProduct && (
          <div className="modal-overlay">
            <div className="modal-content">
              <h2>Edit Product</h2>
              <form onSubmit={handleUpdate} className="product-form">
                <label className="form-field">
                  <span>Category</span>
                  <select 
                    value={editCategoryId} 
                    onChange={(e) => setEditCategoryId(e.target.value)}
                  >
                    <option value="" disabled>Select category</option>
                    {categories.map((cat) => (
                      <option key={cat.id} value={cat.id}>
                        {cat.title}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="form-field">
                  <span>Product Name</span>
                  <input type="text" value={editName} onChange={(e) => setEditName(e.target.value)} />
                </label>
                <label className="form-field">
                  <span>Description</span>
                  <textarea rows={3} value={editDescription} onChange={(e) => setEditDescription(e.target.value)} />
                </label>
                <div className="form-row-2">
                  <label className="form-field">
                    <span>Price ($)</span>
                    <input type="number" step="0.01" value={editPrice} onChange={(e) => setEditPrice(e.target.value)} />
                  </label>
                  <label className="form-field">
                    <span>Stock</span>
                    <input type="number" value={editStock} onChange={(e) => setEditStock(e.target.value)} />
                  </label>
                </div>
                <label className="form-field">
                  <span>SKU</span>
                  <input type="text" value={editSku} onChange={(e) => setEditSku(e.target.value)} />
                </label>
                <label className="form-field">
                  <span>Replace Image</span>
                  <input type="file" ref={editFileInputRef} accept="image/*" onChange={(e) => setEditImageFile(e.target.files?.[0] || null)} />
                </label>
                <div className="modal-actions">
                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Saving...' : 'Save Changes'}
                  </button>
                  <button type="button" className="button button--quiet" onClick={() => setEditingProduct(null)}>
                    Cancel
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </main>
  )
}