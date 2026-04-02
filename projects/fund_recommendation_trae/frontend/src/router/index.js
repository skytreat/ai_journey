import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'home',
    component: () => import('../views/HomeView.vue'),
    meta: { title: '首页' }
  },
  {
    path: '/funds',
    name: 'funds',
    component: () => import('../views/FundList.vue'),
    meta: { title: '基金列表' }
  },
  {
    path: '/fund/:code',
    name: 'fundDetail',
    component: () => import('../views/FundDetail.vue'),
    meta: { title: '基金详情' }
  },
  {
    path: '/analysis',
    name: 'analysis',
    component: () => import('../views/FundAnalysis.vue'),
    meta: { title: '基金分析' }
  },
  {
    path: '/compare',
    name: 'compare',
    component: () => import('../views/CompareView.vue'),
    meta: { title: '基金对比' }
  },
  {
    path: '/favorites',
    name: 'favorites',
    component: () => import('../views/FavoritesView.vue'),
    meta: { title: '自选基金' }
  },
  {
    path: '/query',
    name: 'query',
    component: () => import('../views/QueryView.vue'),
    meta: { title: '自定义查询' }
  },
  {
    path: '/settings',
    name: 'settings',
    component: () => import('../views/SettingsView.vue'),
    meta: { title: '系统设置' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  document.title = to.meta.title ? `${to.meta.title} - 基金信息管理分析系统` : '基金信息管理分析系统'
  next()
})

export default router
