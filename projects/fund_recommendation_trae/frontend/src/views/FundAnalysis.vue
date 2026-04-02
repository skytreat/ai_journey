<template>
  <div class="fund-analysis-container">
    <el-card class="analysis-header">
      <template #header>
        <div class="card-header">
          <span>基金分析</span>
        </div>
      </template>
      <p>通过多种分析方法评估基金表现，帮助您做出更明智的投资决策。</p>
    </el-card>

    <!-- 分析选项卡 -->
    <el-card class="analysis-content">
      <el-tabs v-model="activeTab" type="border-card">
        <!-- 单周期排名 -->
        <el-tab-pane label="单周期排名" name="ranking">
          <div class="tab-content">
            <el-form :inline="true" class="ranking-form">
              <el-form-item label="时间周期">
                <el-select v-model="rankingParams.period" placeholder="选择周期">
                  <el-option label="1个月" value="month" />
                  <el-option label="3个月" value="quarter" />
                  <el-option label="6个月" value="halfyear" />
                  <el-option label="1年" value="year" />
                </el-select>
              </el-form-item>
              <el-form-item label="排序方式">
                <el-select v-model="rankingParams.order" placeholder="选择排序">
                  <el-option label="从高到低" value="desc" />
                  <el-option label="从低到高" value="asc" />
                </el-select>
              </el-form-item>
              <el-form-item label="显示数量">
                <el-select v-model="rankingParams.limit" placeholder="选择数量">
                  <el-option label="10" value="10" />
                  <el-option label="20" value="20" />
                  <el-option label="50" value="50" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="fetchRanking">查询</el-button>
              </el-form-item>
            </el-form>

            <el-table :data="rankingData" style="width: 100%" v-loading="loading.ranking">
              <el-table-column prop="rank" label="排名" width="80" />
              <el-table-column prop="code" label="基金代码" width="100" />
              <el-table-column prop="name" label="基金名称" min-width="180" />
              <el-table-column prop="fundType" label="基金类型" width="100" />
              <el-table-column prop="returnRate" label="收益率" width="120">
                <template #default="scope">
                  <span :class="scope.row.returnRate >= 0 ? 'positive' : 'negative'">
                    {{ scope.row.returnRate >= 0 ? '+' : '' }}{{ (scope.row.returnRate * 100).toFixed(2) }}%
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="nav" label="最新净值" width="100" />
              <el-table-column label="操作" width="100">
                <template #default="scope">
                  <el-button type="primary" size="small" @click="goToFundDetail(scope.row.code)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- 周期变化率排名 -->
        <el-tab-pane label="周期变化率" name="change">
          <div class="tab-content">
            <el-form :inline="true" class="change-form">
              <el-form-item label="时间周期">
                <el-select v-model="changeParams.period" placeholder="选择周期">
                  <el-option label="1个月" value="month" />
                  <el-option label="3个月" value="quarter" />
                  <el-option label="6个月" value="halfyear" />
                  <el-option label="1年" value="year" />
                </el-select>
              </el-form-item>
              <el-form-item label="变化类型">
                <el-select v-model="changeParams.type" placeholder="选择类型">
                  <el-option label="绝对变化" value="absolute" />
                  <el-option label="相对变化" value="relative" />
                </el-select>
              </el-form-item>
              <el-form-item label="显示数量">
                <el-select v-model="changeParams.limit" placeholder="选择数量">
                  <el-option label="10" value="10" />
                  <el-option label="20" value="20" />
                  <el-option label="50" value="50" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="fetchChange">查询</el-button>
              </el-form-item>
            </el-form>

            <el-table :data="changeData" style="width: 100%" v-loading="loading.change">
              <el-table-column prop="rank" label="排名" width="80" />
              <el-table-column prop="code" label="基金代码" width="100" />
              <el-table-column prop="name" label="基金名称" min-width="180" />
              <el-table-column prop="fundType" label="基金类型" width="100" />
              <el-table-column prop="changeValue" label="变化值" width="120" />
              <el-table-column prop="changeRate" label="变化率" width="120">
                <template #default="scope">
                  <span :class="scope.row.changeRate >= 0 ? 'positive' : 'negative'">
                    {{ scope.row.changeRate >= 0 ? '+' : '' }}{{ (scope.row.changeRate * 100).toFixed(2) }}%
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="100">
                <template #default="scope">
                  <el-button type="primary" size="small" @click="goToFundDetail(scope.row.code)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- 多周期一致性筛选 -->
        <el-tab-pane label="一致性分析" name="consistency">
          <div class="tab-content">
            <el-form :inline="true" class="consistency-form">
              <el-form-item label="开始日期">
                <el-date-picker
                  v-model="consistencyParams.startDate"
                  type="date"
                  placeholder="选择开始日期"
                  format="YYYY-MM-DD"
                  value-format="YYYY-MM-DD"
                />
              </el-form-item>
              <el-form-item label="结束日期">
                <el-date-picker
                  v-model="consistencyParams.endDate"
                  type="date"
                  placeholder="选择结束日期"
                  format="YYYY-MM-DD"
                  value-format="YYYY-MM-DD"
                />
              </el-form-item>
              <el-form-item label="显示数量">
                <el-select v-model="consistencyParams.limit" placeholder="选择数量">
                  <el-option label="10" value="10" />
                  <el-option label="20" value="20" />
                  <el-option label="50" value="50" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="fetchConsistency">查询</el-button>
              </el-form-item>
            </el-form>

            <el-table :data="consistencyData" style="width: 100%" v-loading="loading.consistency">
              <el-table-column prop="code" label="基金代码" width="100" />
              <el-table-column prop="name" label="基金名称" min-width="180" />
              <el-table-column prop="fundType" label="基金类型" width="100" />
              <el-table-column prop="consistencyScore" label="一致性得分" width="120">
                <template #default="scope">
                  <el-progress :percentage="scope.row.consistencyScore" :color="getScoreColor(scope.row.consistencyScore)" />
                </template>
              </el-table-column>
              <el-table-column prop="averageReturn" label="平均收益" width="120">
                <template #default="scope">
                  <span :class="scope.row.averageReturn >= 0 ? 'positive' : 'negative'">
                    {{ scope.row.averageReturn >= 0 ? '+' : '' }}{{ (scope.row.averageReturn * 100).toFixed(2) }}%
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="100">
                <template #default="scope">
                  <el-button type="primary" size="small" @click="goToFundDetail(scope.row.code)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- 多因子量化评估 -->
        <el-tab-pane label="多因子评估" name="multifactor">
          <div class="tab-content">
            <el-form :inline="true" class="multifactor-form">
              <el-form-item label="显示数量">
                <el-select v-model="multifactorParams.limit" placeholder="选择数量">
                  <el-option label="10" value="10" />
                  <el-option label="20" value="20" />
                  <el-option label="50" value="50" />
                </el-select>
              </el-form-item>
              <el-form-item label="评估因子">
                <el-select v-model="multifactorParams.factors" multiple placeholder="选择因子">
                  <el-option label="收益" value="return" />
                  <el-option label="风险" value="risk" />
                  <el-option label="风险调整收益" value="riskAdjustedReturn" />
                  <el-option label="排名" value="ranking" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="fetchMultiFactor">查询</el-button>
              </el-form-item>
            </el-form>

            <el-table :data="multifactorData" style="width: 100%" v-loading="loading.multifactor">
              <el-table-column prop="code" label="基金代码" width="100" />
              <el-table-column prop="name" label="基金名称" min-width="180" />
              <el-table-column prop="fundType" label="基金类型" width="100" />
              <el-table-column prop="totalScore" label="总得分" width="120">
                <template #default="scope">
                  <el-progress :percentage="scope.row.totalScore" :color="getScoreColor(scope.row.totalScore)" />
                </template>
              </el-table-column>
              <el-table-column prop="scores" label="因子得分">
                <template #default="scope">
                  <div class="factor-scores">
                    <div class="factor-item">
                      <span class="factor-label">收益:</span>
                      <el-progress :percentage="scope.row.scores.returnScore" :color="getScoreColor(scope.row.scores.returnScore)" :stroke-width="6" />
                    </div>
                    <div class="factor-item">
                      <span class="factor-label">风险:</span>
                      <el-progress :percentage="scope.row.scores.riskScore" :color="getScoreColor(scope.row.scores.riskScore)" :stroke-width="6" />
                    </div>
                    <div class="factor-item">
                      <span class="factor-label">风险调整收益:</span>
                      <el-progress :percentage="scope.row.scores.riskAdjustedReturnScore" :color="getScoreColor(scope.row.scores.riskAdjustedReturnScore)" :stroke-width="6" />
                    </div>
                    <div class="factor-item">
                      <span class="factor-label">排名:</span>
                      <el-progress :percentage="scope.row.scores.rankingScore" :color="getScoreColor(scope.row.scores.rankingScore)" :stroke-width="6" />
                    </div>
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="100">
                <template #default="scope">
                  <el-button type="primary" size="small" @click="goToFundDetail(scope.row.code)">详情</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- 基金对比 -->
        <el-tab-pane label="基金对比" name="compare">
          <div class="tab-content">
            <el-form class="compare-form">
              <el-form-item label="基金代码">
                <el-tag v-for="fundCode in compareParams.fundIds" :key="fundCode" closable @close="removeFund(fundCode)">
                  {{ fundCode }}
                </el-tag>
                <el-input v-model="newFundCode" placeholder="输入基金代码" style="width: 200px; margin-left: 10px;" />
                <el-button type="primary" @click="addFund">添加</el-button>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="fetchCompare" :disabled="compareParams.fundIds.length < 2">对比</el-button>
              </el-form-item>
            </el-form>

            <el-table :data="compareData" style="width: 100%" v-loading="loading.compare">
              <el-table-column prop="fundId" label="基金代码" width="100" />
              <el-table-column prop="fundName" label="基金名称" min-width="180" />
              <el-table-column prop="fundType" label="基金类型" width="100" />
              <el-table-column prop="nav" label="最新净值" width="100" />
              <el-table-column prop="accumulatedNav" label="累计净值" width="100" />
              <el-table-column prop="monthlyReturn" label="近1月收益" width="120">
                <template #default="scope">
                  <span :class="scope.row.monthlyReturn >= 0 ? 'positive' : 'negative'">
                    {{ scope.row.monthlyReturn >= 0 ? '+' : '' }}{{ (scope.row.monthlyReturn * 100).toFixed(2) }}%
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="quarterlyReturn" label="近3月收益" width="120">
                <template #default="scope">
                  <span :class="scope.row.quarterlyReturn >= 0 ? 'positive' : 'negative'">
                    {{ scope.row.quarterlyReturn >= 0 ? '+' : '' }}{{ (scope.row.quarterlyReturn * 100).toFixed(2) }}%
                  </span>
                </template>
              </el-table-column>
              <el-table-column prop="yearlyReturn" label="近1年收益" width="120">
                <template #default="scope">
                  <span :class="scope.row.yearlyReturn >= 0 ? 'positive' : 'negative'">
                    {{ scope.row.yearlyReturn >= 0 ? '+' : '' }}{{ (scope.row.yearlyReturn * 100).toFixed(2) }}%
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
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useFundStore } from '../store/fundStore'

const router = useRouter()
const fundStore = useFundStore()

// 激活的选项卡
const activeTab = ref('ranking')

// 加载状态
const loading = ref({
  ranking: false,
  change: false,
  consistency: false,
  multifactor: false,
  compare: false
})

// 单周期排名参数
const rankingParams = ref({
  period: 'month',
  limit: '10',
  order: 'desc'
})

// 周期变化率参数
const changeParams = ref({
  period: 'month',
  limit: '10',
  type: 'absolute'
})

// 一致性分析参数
const consistencyParams = ref({
  startDate: '2023-01-01',
  endDate: '2024-01-01',
  limit: '10'
})

// 多因子评估参数
const multifactorParams = ref({
  limit: '10',
  factors: []
})

// 基金对比参数
const compareParams = ref({
  fundIds: []
})
const newFundCode = ref('')

// 数据
const rankingData = ref([])
const changeData = ref([])
const consistencyData = ref([])
const multifactorData = ref([])
const compareData = ref([])

// 跳转到基金详情页面
const goToFundDetail = (code) => {
  router.push(`/fund/${code}`)
}

// 获取单周期排名
const fetchRanking = async () => {
  loading.value.ranking = true
  try {
    const response = await fundStore.getFundRanking(
      rankingParams.value.period,
      parseInt(rankingParams.value.limit),
      rankingParams.value.order
    )
    rankingData.value = response.rankings
  } catch (error) {
    console.error('获取基金排名失败:', error)
    ElMessage.error('获取基金排名失败')
  } finally {
    loading.value.ranking = false
  }
}

// 获取周期变化率
const fetchChange = async () => {
  loading.value.change = true
  try {
    const response = await fundStore.getFundChangeRanking(
      changeParams.value.period,
      parseInt(changeParams.value.limit),
      changeParams.value.type
    )
    changeData.value = response.rankings
  } catch (error) {
    console.error('获取周期变化率失败:', error)
    ElMessage.error('获取周期变化率失败')
  } finally {
    loading.value.change = false
  }
}

// 获取一致性分析
const fetchConsistency = async () => {
  loading.value.consistency = true
  try {
    const response = await fundStore.getFundConsistency(
      consistencyParams.value.startDate,
      consistencyParams.value.endDate,
      parseInt(consistencyParams.value.limit)
    )
    consistencyData.value = response.funds
  } catch (error) {
    console.error('获取一致性分析失败:', error)
    ElMessage.error('获取一致性分析失败')
  } finally {
    loading.value.consistency = false
  }
}

// 获取多因子评估
const fetchMultiFactor = async () => {
  loading.value.multifactor = true
  try {
    const response = await fundStore.getFundMultiFactorScore(
      parseInt(multifactorParams.value.limit),
      multifactorParams.value.factors
    )
    multifactorData.value = response.funds
  } catch (error) {
    console.error('获取多因子评估失败:', error)
    ElMessage.error('获取多因子评估失败')
  } finally {
    loading.value.multifactor = false
  }
}

// 添加基金到对比列表
const addFund = () => {
  if (!newFundCode.value) {
    ElMessage.warning('请输入基金代码')
    return
  }
  if (compareParams.value.fundIds.includes(newFundCode.value)) {
    ElMessage.warning('该基金已在对比列表中')
    return
  }
  compareParams.value.fundIds.push(newFundCode.value)
  newFundCode.value = ''
}

// 从对比列表中移除基金
const removeFund = (fundCode) => {
  const index = compareParams.value.fundIds.indexOf(fundCode)
  if (index > -1) {
    compareParams.value.fundIds.splice(index, 1)
  }
}

// 获取基金对比
const fetchCompare = async () => {
  if (compareParams.value.fundIds.length < 2) {
    ElMessage.warning('请至少添加两个基金进行对比')
    return
  }
  
  loading.value.compare = true
  try {
    const response = await fundStore.compareFunds(compareParams.value.fundIds)
    compareData.value = response.funds
  } catch (error) {
    console.error('获取基金对比失败:', error)
    ElMessage.error('获取基金对比失败')
  } finally {
    loading.value.compare = false
  }
}

// 获取得分颜色
const getScoreColor = (score) => {
  if (score >= 80) return '#67c23a'
  if (score >= 60) return '#e6a23c'
  return '#f56c6c'
}

// 初始化
onMounted(() => {
  fetchRanking()
})
</script>

<style scoped>
.fund-analysis-container {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.analysis-header,
.analysis-content {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.tab-content {
  padding: 20px 0;
}

.ranking-form,
.change-form,
.consistency-form,
.multifactor-form,
.compare-form {
  margin-bottom: 20px;
}

.factor-scores {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.factor-item {
  display: flex;
  align-items: center;
  gap: 10px;
}

.factor-label {
  width: 80px;
  font-size: 12px;
}

.positive {
  color: #67c23a;
}

.negative {
  color: #f56c6c;
}

@media (max-width: 768px) {
  .fund-analysis-container {
    padding: 10px;
  }
  
  .factor-item {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .factor-label {
    width: 100%;
  }
}
</style>
