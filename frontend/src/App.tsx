import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import LoginPage from './pages/loginPage'
import RegisterPage from './pages/registerPage'
import HomePage from './pages/homePage'
import MainPage from './pages/mainPage'
import ProductDetailPage from './pages/productDetailPage'
import SellerProfilePage from './pages/sellerProfilePage'
import CartPage from './pages/cartPage'
import OrdersPage from './pages/orderPage'
import NotificationsPage from './pages/notificationPage'
import OrderDetailPage from './pages/orderDetailPage'
import CheckoutPaymentPage from './pages/checkoutPaymentPage'
import SellerProfileDetailPage from './pages/sellerProfileDetailPage'
import SellerOrderItemsDetailPage from './pages/sellerOrderItemsPage'
import SellerProductsPage from './pages/sellerProductPage'
import UserProfilePage from './pages/userProfilePage'
import AdminUsersPage from './pages/adminUserPage'
import AdminUserDetailPage from './pages/adminUserDetailPage'
import AdminCategoriesPage from './pages/adminCategoriesPage'
import AdminCategoryDetailPage from './pages/adminCategoryDetailPage'


export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/main" element={<MainPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/product/:id" element={<ProductDetailPage />} />
        <Route path="/seller/profile" element={<SellerProfilePage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/orders/:id" element={<OrderDetailPage />} />
        <Route path="/payment/:orderId" element={<CheckoutPaymentPage />} />
        <Route path="/seller/:id" element={<SellerProfileDetailPage />} />
        <Route path="/seller/order/:id/detail" element={<SellerOrderItemsDetailPage />} />
        <Route path="/seller/products" element={<SellerProductsPage />} />
        <Route path="/user/profile" element={<UserProfilePage />} />
        <Route path="/admin/users" element={<AdminUsersPage />} />
        <Route path="/admin/users/:id" element={<AdminUserDetailPage />} />
        <Route path="/admin/categories" element={<AdminCategoriesPage />} />
        <Route path="/admin/categories/:id" element={<AdminCategoryDetailPage />} />
        
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}