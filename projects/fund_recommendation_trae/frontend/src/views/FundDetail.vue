<template>
  <div class="fund-detail-container">
    <el-card class="fund-detail-header">
      <template #header>
        <div class="card-header">
          <span>基金详情</span>
          <el-button type="success" @click="addToFavorites">收藏</el-button>
        </div>
      </template>
      
      <!-- 基金基本信息 -->
      <div class="fund-basic-info" v-if="fundDetail">
        <el-row :gutter="20">
          <el-col :span="8">
            <div class="info-item">
              <span class="info-label">基金代码：</span>
              <span class="info-value">{{ fundDetail.code }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">基金名称：</span>
              <span class="info-value">{{ fundDetail.name }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">基金类型：</span>
              <span class="info-value">{{ fundDetail.fundType }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">成立日期：</span>
              <span class="info-value">{{ fundDetail.establishDate }}</span>
            </div>
          </el-col>
          <el-col :span="8">
            <div class="info-item">
              <span class="info-label">基金经理：</span>
              <span class="info-value">{{ fundDetail.manager }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">风险等级：</span>
              <span class="info-value">{{ fundDetail.riskLevel }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">最新净值：</span>
              <span class="info-value">{{ fundDetail.nav }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">日涨跌幅：</span>
              <span class="info-value" :class="fundDetail.dailyGrowthRate >= 0 ? 'positive' : 'negative'">
                {{ fundDetail.dailyGrowthRate >= 0 ? '+' : '' }}{{ fundDetail.dailyGrowthRate }}%
              </span>
            </div>
          </el-col>
          <el-col :span="8">
            <div class="info-item">
              <span class="info-label">累计净值：</span>
              <span class="info-value">{{ fundDetail.accumulatedNav }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">近1月收益：</span>
              <span class="info-value" :class="fundDetail.monthlyReturn >= 0 ? 'positive' : 'negative'">
                {{ fundDetail.monthlyReturn >= 0 ? '+' : '' }}{{ fundDetail.monthlyReturn }}%
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">近1年收益：</span>
              <span class="info-value" :class="fundDetail.yearlyReturn >= 0 ? 'positive' : 'negative'">
                {{ fundDetail.yearlyReturn >= 0 ? '+' : '' }}{{ fundDetail.yearlyReturn }}%
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">夏普比率：</span>
              <span class="info-value">{{ fundDetail.sharpeRatio }}</span>
            </div>
          </el-col>
        </el-row>
      </div>
    </el-card>

    <!-- 净值走势 -->
    <el-card class="fund-nav-chart" v-if="fundDetail">
      <template #header>
        <div class="card-header">
          <span>净值走势</span>
          <el-select v-model="navPeriod" placeholder="选择周期" size="small">
            <el-option label="近1个月" value="1m" />
            <el-option label="近3个月" value="3m" />
            <el-option label="近6个月" value="6m" />
            <el-option label="近1年" value="1y" />
            <el-option label="近3年" value="3y" />
          </el-select>
        </div>
      </template>
      <div id="navChart" class="chart-container"></div>
    </el-card>

    <!-- 业绩指标 -->
    <el-card class="fund-performance" v-if="fundDetail">
      <template #header>
        <div class="card-header">
          <span>业绩指标</span>
        </div>
      </template>
      <el-table :data="fundPerformance" style="width: 100%">
        <el-table-column prop="periodType" label="时间周期" width="120" />
        <el-table-column prop="navGrowthRate" label="净值增长率" width="120">
          <template #default="scope">
            <span :class="scope.row.navGrowthRate >= 0 ? 'positive' : 'negative'">
              {{ scope.row.navGrowthRate >= 0 ? '+' : '' }}{{ (scope.row.navGrowthRate * 100).toFixed(2) }}%
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="maxDrawdown" label="最大回撤" width="120">
          <template #default="scope">
            <span class="negative">{{ (scope.row.maxDrawdown * 100).toFixed(2) }}%</span>
          </template>
        </el-table-column>
        <el-table-column prop="sharpeRatio" label="夏普比率" width="100" />
      </el-table>
    </el-card>

    <!-- 基金经理信息 -->
    <el-card class="fund-managers" v-if="fundDetail">
      <template #header>
        <div class="card-header">
          <span>基金经理信息</span>
        </div>
      </template>
      <el-table :data="fundManagers" style="width: 100%">
        <el-table-column prop="managerName" label="基金经理" width="120" />
        <el-table-column prop="tenure" label="任职年限" width="100" />
        <el-table-column prop="startDate" label="任职开始日期" width="150" />
        <el-table-column prop="endDate" label="任职结束日期" width="150" />
      </el-table>
    </el-card>

    <!-- 基金规模 -->
    <el-card class="fund-scale" v-if="fundDetail">
      <template #header>
        <div class="card-header">
          <span>基金规模</span>
        </div>
      </template>
      <div id="scaleChart" class="chart-container"></div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import { useFundStore } from '../store/fundStore'

const route = useRoute()
const router = useRouter()
const fundStore = useFundStore()

// 基金代码
const fundCode = ref(route.params.code)

// 基金详情
const fundDetail = ref(null)
const fundPerformance = ref([])
const fundManagers = ref([])
const fundScale = ref([])

// 净值周期
const navPeriod = ref('1y')

// 图表实例
let navChart = null
let scaleChart = null

// 添加到自选基金
const addToFavorites = () => {
  fundStore.addFavorite(fundCode.value)
  ElMessage.success('添加成功')
}

// 获取基金详情
const fetchFundDetail = async () => {
  try {
    const detail = await fundStore.getFundDetail(fundCode.value)
    fundDetail.value = detail
  } catch (error) {
    console.error('获取基金详情失败:', error)
    ElMessage.error('获取基金详情失败')
  }
}

// 获取基金历史净值
const fetchFundNav = async () => {
  try {
    const navHistory = await fundStore.getFundNav(fundCode.value)
    renderNavChart(navHistory.navHistory)
  } catch (error) {
    console.error('获取基金净值失败:', error)
  }
}

// 获取基金业绩指标
const fetchFundPerformance = async () => {
  try {
    const performance = await fundStore.getFundPerformance(fundCode.value)
    fundPerformance.value = performance.performances
  } catch (error) {
    console.error('获取基金业绩失败:', error)
  }
}

// 获取基金经理信息
const fetchFundManagers = async () => {
  try {
    const managers = await fundStore.getFundManagers(fundCode.value)
    fundManagers.value = managers.managers
  } catch (error) {
    console.error('获取基金经理信息失败:', error)
  }
}

// 获取基金规模
const fetchFundScale = async () => {
  try {
    const scale = await fundStore.getFundScale(fundCode.value)
    fundScale.value = scale.scales
    renderScaleChart(scale.scales)
  } catch (error) {
    console.error('获取基金规模失败:', error)
  }
}

// 渲染净值走势图
const renderNavChart = (navHistory) => {
  const chartDom = document.getElementById('navChart')
  if (!chartDom) return
  
  if (navChart) {
    navChart.dispose()
  }
  
  navChart = echarts.init(chartDom)
  
  const dates = navHistory.map(item => item.date)
  const nav = navHistory.map(item => item.nav)
  const adjustedNav = navHistory.map(item => item.adjustedNav)
  
  const option = {
    title: {
      text: '净值走势',
      left: 'center'
    },
    tooltip: {
      trigger: 'axis'
    },
    legend: {
      data: ['单位净值', '复权净值'],
      bottom: 0
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '15%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: dates
    },
    yAxis: {
      type: 'value'
    },
    series: [
      {
        name: '单位净值',
        type: 'line',
        data: nav
      },
      {
        name: '复权净值',
        type: 'line',
        data: adjustedNav
      }
    ]
  }
  
  navChart.setOption(option)
  
  // 响应式调整
  window.addEventListener('resize', () => {
    navChart.resize()
  })
}

// 渲染规模走势图
const renderScaleChart = (scales) => {
  const chartDom = document.getElementById('scaleChart')
  if (!chartDom) return
  
  if (scaleChart) {
    scaleChart.dispose()
  }
  
  scaleChart = echarts.init(chartDom)
  
  const dates = scales.map(item => item.date)
  const values = scales.map(item => item.assetScale)
  
  const option = {
    title: {
      text: '基金规模变化',
      left: 'center'
    },
    tooltip: {
      trigger: 'axis'
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: dates
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        formatter: '{value} 万元'
      }
    },
    series: [
      {
        name: '基金规模',
        type: 'line',
        data: values
      }
    ]
  }
  
  scaleChart.setOption(option)
  
  // 响应式调整
  window.addEventListener('resize', () => {
    scaleChart.resize()
  })
}

// 初始化
onMounted(() => {
  fetchFundDetail()
  fetchFundNav()
  fetchFundPerformance()
  fetchFundManagers()
  fetchFundScale()
})

// 监听周期变化
watch(navPeriod, () => {
  fetchFundNav()
})

// 监听路由参数变化
watch(() => route.params.code, (newCode) => {
  if (newCode) {
    fundCode.value = newCode
    fetchFundDetail()
    fetchFundNav()
    fetchFundPerformance()
    fetchFundManagers()
    fetchFundScale()
  }
})
</script>

<style scoped>
.fund-detail-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.fund-detail-header,
.fund-nav-chart,
.fund-performance,
.fund-managers,
.fund-scale {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.fund-basic-info {
  padding: 20px 0;
}

.info-item {
  margin-bottom: 10px;
  display: flex;
  align-items: center;
}

.info-label {
  width: 100px;
  color: #666;
}

.info-value {
  font-weight: 500;
}

.positive {
  color: #67c23a;
}

.negative {
  color: #f56c6c;
}

.chart-container {
  width: 100%;
  height: 400px;
}

@media (max-width: 768px) {
  .fund-detail-container {
    padding: 10px;
  }
  
  .chart-container {
    height: 300px;
  }
  
  .info-item {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .info-label {
    width: 100%;
    margin-bottom: 5px;
  }
}
</style>
