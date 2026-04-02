<template>
  <div class="home-container">
    <!-- 页面标题 -->
    <el-card class="home-header">
      <template #header>
        <div class="card-header">
          <span>基金信息管理分析系统</span>
        </div>
      </template>
      <div class="header-content">
        <p>欢迎使用基金信息管理分析系统，您可以通过本系统查询基金信息、分析基金业绩、管理自选基金等。</p>
      </div>
    </el-card>

    <!-- 系统概览 -->
    <el-card class="overview-card">
      <template #header>
        <div class="card-header">
          <span>系统概览</span>
        </div>
      </template>
      <div class="overview-stats">
        <el-statistic title="基金总数" :value="fundCount" />
        <el-statistic title="数据更新时间" :value="lastUpdateTime" />
        <el-statistic title="系统状态" :value="systemStatus" />
      </div>
    </el-card>

    <!-- 市场概览 -->
    <el-card class="market-card">
      <template #header>
        <div class="card-header">
          <span>市场概览</span>
        </div>
      </template>
      <div class="market-trends">
        <div id="marketChart" class="chart-container"></div>
      </div>
    </el-card>

    <!-- 自选基金快览 -->
    <el-card class="favorites-card">
      <template #header>
        <div class="card-header">
          <span>自选基金快览</span>
          <el-button type="primary" size="small" @click="goToFavorites">查看全部</el-button>
        </div>
      </template>
      <div class="favorites-list">
        <el-table :data="favoriteFunds" style="width: 100%">
          <el-table-column prop="code" label="基金代码" width="120" />
          <el-table-column prop="name" label="基金名称" />
          <el-table-column prop="nav" label="最新净值" width="100" />
          <el-table-column prop="dailyReturn" label="日涨跌幅" width="120" />
          <el-table-column label="操作" width="100">
            <template #default="scope">
              <el-button type="text" @click="goToFundDetail(scope.row.code)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import * as echarts from 'echarts'

const router = useRouter()

// 模拟数据
const fundCount = ref(1000)
const lastUpdateTime = ref('2024-01-01 12:00:00')
const systemStatus = ref('正常')

const favoriteFunds = ref([
  { code: '000001', name: '华夏成长混合', nav: 1.2345, dailyReturn: '+0.5%' },
  { code: '000002', name: '易方达蓝筹精选', nav: 2.3456, dailyReturn: '-0.2%' },
  { code: '000003', name: '嘉实成长收益', nav: 1.5678, dailyReturn: '+1.2%' }
])

// 跳转到自选基金页面
const goToFavorites = () => {
  router.push('/favorites')
}

// 跳转到基金详情页面
const goToFundDetail = (code) => {
  router.push(`/fund/${code}`)
}

// 初始化市场概览图表
onMounted(() => {
  const chartDom = document.getElementById('marketChart')
  const myChart = echarts.init(chartDom)
  
  const option = {
    title: {
      text: '市场趋势',
      left: 'center'
    },
    tooltip: {
      trigger: 'axis'
    },
    legend: {
      data: ['股票型', '混合型', '债券型'],
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
      data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月']
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        formatter: '{value}%'
      }
    },
    series: [
      {
        name: '股票型',
        type: 'line',
        stack: 'Total',
        data: [3.2, 4.5, 5.1, 6.2, 7.5, 8.1, 7.8, 6.5, 5.2, 4.8, 5.5, 6.2]
      },
      {
        name: '混合型',
        type: 'line',
        stack: 'Total',
        data: [2.5, 3.8, 4.2, 5.1, 6.3, 7.1, 6.8, 5.9, 4.8, 4.2, 4.9, 5.5]
      },
      {
        name: '债券型',
        type: 'line',
        stack: 'Total',
        data: [1.2, 1.5, 1.8, 2.1, 2.3, 2.5, 2.4, 2.2, 2.0, 1.8, 2.1, 2.3]
      }
    ]
  }
  
  myChart.setOption(option)
  
  // 响应式调整
  window.addEventListener('resize', () => {
    myChart.resize()
  })
})
</script>

<style scoped>
.home-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.home-header {
  margin-bottom: 20px;
}

.overview-card,
.market-card,
.favorites-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-content {
  padding: 20px 0;
}

.overview-stats {
  display: flex;
  justify-content: space-around;
  flex-wrap: wrap;
}

.market-trends {
  height: 400px;
}

.chart-container {
  width: 100%;
  height: 100%;
}

.favorites-list {
  margin-top: 10px;
}

@media (max-width: 768px) {
  .home-container {
    padding: 10px;
  }
  
  .overview-stats {
    flex-direction: column;
    gap: 20px;
  }
  
  .market-trends {
    height: 300px;
  }
}
</style>
