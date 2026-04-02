import { defineStore } from 'pinia'
import axios from 'axios'

// 创建axios实例
const apiClient = axios.create({
  baseURL: 'http://localhost:5026/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
})

export const useFundStore = defineStore('fund', {
  state: () => ({
    funds: [],
    fundDetail: null,
    navHistory: [],
    performance: [],
    managers: [],
    scales: [],
    loading: false,
    error: null
  }),

  getters: {
    getFunds: (state) => state.funds,
    getFundDetail: (state) => state.fundDetail,
    getNavHistory: (state) => state.navHistory,
    getPerformance: (state) => state.performance,
    getManagers: (state) => state.managers,
    getScales: (state) => state.scales,
    isLoading: (state) => state.loading,
    getError: (state) => state.error
  },

  actions: {
    // 获取基金列表
    async fetchFunds(page = 1, pageSize = 10, fundType = null, riskLevel = null) {
      this.loading = true
      this.error = null
      try {
        const params = {
          page,
          pageSize
        }
        if (fundType) params.fundType = fundType
        if (riskLevel) params.riskLevel = riskLevel

        const response = await apiClient.get('/funds', { params })
        this.funds = response.data
      } catch (error) {
        this.error = error.message
        // 使用模拟数据
        this.funds = {
          total: 100,
          page: 1,
          pageSize: 10,
          funds: Array.from({ length: 10 }, (_, i) => ({
            code: `0000${i + 1}`,
            name: `测试基金${i + 1}`,
            fundType: ['混合型', '股票型', '债券型', '货币型'][i % 4],
            manager: `基金经理${i + 1}`,
            establishDate: `2020-01-0${i + 1}`,
            riskLevel: ['低风险', '中风险', '高风险'][i % 3]
          }))
        }
      } finally {
        this.loading = false
      }
    },

    // 获取基金详情
    async fetchFundDetail(code) {
      this.loading = true
      this.error = null
      try {
        const response = await apiClient.get(`/funds/${code}`)
        this.fundDetail = response.data
      } catch (error) {
        this.error = error.message
        // 使用模拟数据
        this.fundDetail = {
          code,
          name: `测试基金${code.slice(-2)}`,
          fundType: '混合型',
          manager: '基金经理1',
          establishDate: '2020-01-01',
          riskLevel: '中风险',
          currentNav: 1.2345,
          accumulatedNav: 2.3456,
          dailyGrowthRate: 0.005
        }
      } finally {
        this.loading = false
      }
    },

    // 获取基金净值历史
    async fetchNavHistory(code, startDate = null, endDate = null) {
      this.loading = true
      this.error = null
      try {
        const params = {}
        if (startDate) params.startDate = startDate
        if (endDate) params.endDate = endDate

        const response = await apiClient.get(`/funds/${code}/nav`, { params })
        this.navHistory = response.data
      } catch (error) {
        this.error = error.message
        // 使用模拟数据
        this.navHistory = {
          code,
          navHistory: Array.from({ length: 30 }, (_, i) => {
            const date = new Date()
            date.setDate(date.getDate() - (29 - i))
            return {
              date: date.toISOString().split('T')[0],
              nav: 1 + Math.random() * 0.5,
              accumulatedNav: 2 + Math.random() * 0.5,
              dailyGrowthRate: (Math.random() - 0.5) * 0.02,
              adjustedNav: 2 + Math.random() * 0.5
            }
          })
        }
      } finally {
        this.loading = false
      }
    },

    // 获取基金业绩指标
    async fetchPerformance(code) {
      this.loading = true
      this.error = null
      try {
        const response = await apiClient.get(`/funds/${code}/performance`)
        this.performance = response.data
      } catch (error) {
        this.error = error.message
        // 使用模拟数据
        this.performance = {
          code,
          performances: [
            { periodType: '近1周', navGrowthRate: 0.01, maxDrawdown: 0.02, sharpeRatio: 1.2 },
            { periodType: '近1月', navGrowthRate: 0.03, maxDrawdown: 0.05, sharpeRatio: 1.3 },
            { periodType: '近3月', navGrowthRate: 0.08, maxDrawdown: 0.08, sharpeRatio: 1.4 },
            { periodType: '近1年', navGrowthRate: 0.15, maxDrawdown: 0.15, sharpeRatio: 1.5 }
          ]
        }
      } finally {
        this.loading = false
      }
    },

    // 获取基金经理信息
    async fetchManagers(code) {
      this.loading = true
      this.error = null
      try {
        const response = await apiClient.get(`/funds/${code}/managers`)
        this.managers = response.data
      } catch (error) {
        this.error = error.message
        // 使用模拟数据
        this.managers = {
          code,
          managers: [
            { managerName: '基金经理1', tenure: '3年', startDate: '2021-01-01', endDate: null },
            { managerName: '基金经理2', tenure: '2年', startDate: '2022-01-01', endDate: '2023-01-01' }
          ]
        }
      } finally {
        this.loading = false
      }
    },

    // 获取基金规模历史
    async fetchScales(code) {
      this.loading = true
      this.error = null
      try {
        const response = await apiClient.get(`/funds/${code}/scale`)
        this.scales = response.data
      } catch (error) {
        this.error = error.message
        // 使用模拟数据
        this.scales = {
          code,
          scales: Array.from({ length: 12 }, (_, i) => {
            const date = new Date()
            date.setMonth(date.getMonth() - (11 - i))
            return {
              date: date.toISOString().split('T')[0],
              assetScale: 5 + Math.random() * 15
            }
          })
        }
      } finally {
        this.loading = false
      }
    }
  }
})
